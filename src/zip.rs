// SPDX-License-Identifier: GPL-3.0-only

use std::io;
use std::sync::Mutex;
use std::{fs::File, sync::Arc};

use blake3::Hash;
use rayon::iter::{IndexedParallelIterator, IntoParallelIterator, ParallelIterator};
use zip::ZipArchive;

use crate::digest::b3sum;

pub fn hash_files(mut zip: ZipArchive<File>) -> impl IndexedParallelIterator<Item = io::Result<Hash>> {
    let len = zip.len();
    let indices = (0..len)
        .filter(|index| {
            let entry = zip.by_index(*index).map_err(|e| match e {
                zip::result::ZipError::Io(error) => error,
                _ => todo!(),
            });
            entry.map(|zf| zf.is_file()).unwrap_or(true)
        })
        .collect::<Vec<_>>();
    let zip = Arc::new(Mutex::new(zip));
    indices.into_par_iter().map(move |index| {
        let zip = zip.clone();
        let mut mzip = zip.lock().expect("no panics");
        let mut entry = mzip.by_index(index).map_err(|e| match e {
            zip::result::ZipError::Io(error) => error,
            _ => todo!(),
        })?;
        b3sum(&mut entry)
    })
}

#[cfg(test)]
mod tests {
    use std::{fs::File, io::Write};

    use tempfile::{NamedTempFile, TempPath};
    use zip::{write::FileOptions, ZipWriter};

    use super::*;

    fn new_archive_from_spec(
        spec: impl IntoIterator<Item = (impl AsRef<str>, impl AsRef<str>)>,
    ) -> io::Result<TempPath> {
        let mut file = NamedTempFile::new()?;
        let options = FileOptions::default();

        {
            let mut zip = ZipWriter::new(file.as_file_mut());
            for (file, data) in spec {
                if data.as_ref() == "<directory>" {
                    zip.add_directory(file.as_ref(), options)?;
                } else {
                    zip.start_file(file.as_ref(), options)?;
                    zip.write(data.as_ref().as_bytes())?;
                }
            }
            zip.finish()?;
        }

        Ok(file.into_temp_path())
    }

    #[test]
    fn empty_sequence_returns_no_hashes() -> io::Result<()> {
        let spec: [(&str, &str); 0] = [];
        let zip_path = new_archive_from_spec(spec)?;
        let zip = ZipArchive::new(File::open(zip_path)?)?;

        let stream = hash_files(zip);
        let result = stream.count();

        assert_eq!(result, 0);

        Ok(())
    }

    #[test]
    fn sequence_with_one_item_returns_one_hash() -> io::Result<()> {
        let expected = vec!["cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323"];

        let zip_path = new_archive_from_spec([("file 1", "file 1 contents")])?;
        let zip = ZipArchive::new(File::open(zip_path)?)?;

        let stream = hash_files(zip);
        let result = stream
            .map(|o| o.expect("file hashed").to_string())
            .collect::<Vec<_>>();

        assert_eq!(result, expected);
        Ok(())
    }

    #[test]
    fn sequence_with_multiple_items_returns_hashes_in_order() -> io::Result<()> {
        let expected = vec![
            "cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323",
            "7b7e5f11be694b5d2b630bb648b74ce91cee8b56aa773015474a86f451d8f335",
            "9400c17d43042a4546cbb8c251f122888e3ed53403096fde646adcd7370ba21e",
        ];

        let zip_path = new_archive_from_spec([
            ("file 1", "file 1 contents"),
            ("file 2", "file 2 contents"),
            ("file 3", "more test data"),
        ])?;
        let zip = ZipArchive::new(File::open(zip_path)?)?;

        let stream = hash_files(zip);
        let result = stream.collect::<Vec<_>>();
        let result = result
            .into_iter()
            .map(|h| h.expect("file hashed").to_string())
            .collect::<Vec<_>>();

        assert_eq!(result, expected);

        Ok(())
    }

    #[test]
    fn sequence_with_directory_entries_and_files_hashes_only_files() -> io::Result<()> {
        let expected = vec![
            "cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323",
            "7b7e5f11be694b5d2b630bb648b74ce91cee8b56aa773015474a86f451d8f335",
            "9400c17d43042a4546cbb8c251f122888e3ed53403096fde646adcd7370ba21e",
        ];

        let zip_path = new_archive_from_spec([
            ("file 1", "file 1 contents"),
            ("file 2", "file 2 contents"),
            ("directory", "<directory>"),
            ("file 3", "more test data"),
        ])?;
        let zip = ZipArchive::new(File::open(zip_path)?)?;

        let stream = hash_files(zip);
        let result = stream.collect::<Vec<_>>();
        let result = result
            .into_iter()
            .map(|h| h.expect("file hashed").to_string())
            .collect::<Vec<_>>();

        assert_eq!(result, expected);

        Ok(())
    }
}
