// SPDX-License-Identifier: GPL-3.0-only

use std::io::{self, Error, ErrorKind};
use std::iter::once;
use std::{cmp::Ordering, ffi::OsStr, fs::File, path::PathBuf};

use blake3::Hash;
use futures_util::{stream, StreamExt};
use itertools::Itertools;

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

impl Difference {
    fn render(&self) -> String {
        let rendered_filename = self
            .path
            .file_name()
            .unwrap_or(OsStr::new("<no filename>"))
            .to_string_lossy();
        match self.kind {
            DifferenceKind::Mismatch => format!("- “{}” does not match", rendered_filename),
            DifferenceKind::Missing => format!("- “{}” is missing", rendered_filename),
        }
    }
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

pub async fn verify(options: Opts) -> io::Result<()> {
    let input = options.input;
    let root = options.root;

    let mut archive = ZipArchive::new(File::open(input)?)?;
    let files = file_names(&mut archive)?;

    let paths = stream::iter(files.clone());
    let fs_checksums = fs::hash_files(files.into_iter().map(|file| root.join(file)));
    let zip_checksums = crate::zip::hash_files(archive);

    let mut differences = paths
        .zip(fs_checksums)
        .zip(zip_checksums)
        .filter_map(|((path, fs_result), zip_result)| process_results(path, fs_result, zip_result))
        .collect::<Vec<_>>()
        .await;

    differences.sort_by(|lhs, rhs| match lhs.path.parent().cmp(&rhs.path.parent()) {
        Ordering::Equal => lhs.path.file_name().cmp(&rhs.path.file_name()),
        result => result,
    });

    let groups = differences
        .into_iter()
        .group_by(|diff| {
            diff.path
                .parent()
                .map(|p| p.to_owned())
                .unwrap_or("/".into())
        })
        .into_iter()
        .map(|(path, differences)| {
            once(path.to_string_lossy().into_owned())
                .chain(differences.map(|d| d.render()))
                .join("\n")
        })
        .join("\n\n");
    println!("{}", groups);
    Ok(())
}
