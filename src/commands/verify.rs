// SPDX-License-Identifier: GPL-3.0-only

use std::{fs::File, path::PathBuf};

use blake3::Hash;
use futures_util::{stream, StreamExt};
use itertools::Itertools;
use tokio::io::{self, Error, ErrorKind};
use zip::{
    result::{ZipError, ZipResult},
    ZipArchive,
};

use crate::{fs, opts::Opts};

fn file_names(archive: &mut ZipArchive<File>) -> ZipResult<Vec<PathBuf>> {
    let mut result = Vec::new();
    for index in 0..archive.len() {
        let zf = archive.by_index(index)?;
        if zf.is_file() {
            if let Some(path) = zf.enclosed_name() {
                result.push(path.to_owned());
            } else {
                return Err(ZipError::InvalidArchive(Box::leak(
                    format!(
                        "archive contains dangerous path, aborting.\n- {}",
                        zf.name()
                    )
                    .into_boxed_str(),
                )));
            }
        }
    }
    Ok(result)
}

struct Difference {
    path: PathBuf,
    kind: DifferenceKind,
}

enum DifferenceKind {
    Mismatch,
    Missing,
}

async fn process_results(
    path: PathBuf,
    fs: Result<Hash, Error>,
    zip: Result<Hash, Error>,
) -> Option<Difference> {
    match (fs, zip) {
        (Ok(fs_hash), Ok(zip_hash)) if fs_hash == zip_hash => None,
        (Err(error), _) if error.kind() == ErrorKind::NotFound => Some(Difference {
            path: path,
            kind: DifferenceKind::Missing,
        }),
        _ => Some(Difference {
            path: path,
            kind: DifferenceKind::Mismatch,
        }),
    }
}

fn print_difference(diff: Difference) {
    match diff {
        Difference {
            path,
            kind: DifferenceKind::Mismatch,
        } => println!("“{}” does not match", path.to_string_lossy()),
        Difference {
            path,
            kind: DifferenceKind::Missing,
        } => println!("“{}” is missing", path.to_string_lossy()),
    }
}

pub async fn verify(options: Opts) -> io::Result<()> {
    let mut archive = ZipArchive::new(File::open(options.input)?)?;
    let files = file_names(&mut archive)?;
    let paths = stream::iter(files.clone());
    let fs_checksums = fs::hash_files(files);
    let zip_checksums = crate::zip::hash_files(archive);
    let differences = paths
        .zip(fs_checksums)
        .zip(zip_checksums)
        .filter_map(|((path, fs_result), zip_result)| process_results(path, fs_result, zip_result))
        .collect::<Vec<_>>()
        .await;
    let groups = differences.into_iter().group_by(|diff| {
        diff.path
            .parent()
            .map(|p| p.to_owned())
            .unwrap_or("/".into())
    });
    for (path, differences) in groups.into_iter() {
        println!("{}", path.to_string_lossy());
        for difference in differences {
            print_difference(difference);
        }
    }
    Ok(())
}
