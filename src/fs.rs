// SPDX-License-Identifier: GPL-3.0-only

use std::fs::File;
use std::io;
use std::path::Path;

use blake3::Hash;
use rayon::iter::{IndexedParallelIterator, IntoParallelIterator};

use crate::digest::b3sum;

pub fn hash_files(
    paths: impl IndexedParallelIterator<Item = impl AsRef<Path>>,
) -> impl IndexedParallelIterator<Item = io::Result<Hash>> {
    paths
        .into_par_iter()
        .map(|path| match File::open(path.as_ref()) {
            Ok(mut file) => {
                let hash = b3sum(&mut file)?;
                Ok(hash)
            }
            Err(error) => Err(error),
        })
}

#[cfg(test)]
mod tests {
    use std::{io::Write, path::PathBuf};

    use rayon::iter::ParallelIterator;
    use tempfile::{tempdir, TempDir};

    use super::*;

    fn set_up_source_files(
        spec: impl IntoIterator<Item = (impl AsRef<Path>, impl AsRef<str>)>,
    ) -> io::Result<(TempDir, Vec<PathBuf>)> {
        let dir = tempdir()?;
        let mut paths = Vec::new();
        for (file, contents) in spec {
            let path = dir.path().join(file);
            let mut file = File::create(&path)?;
            paths.push(path);
            file.write(contents.as_ref().as_bytes())?;
        }
        Ok((dir, paths))
    }

    #[test]
    fn empty_sequence_returns_no_hashes() -> io::Result<()> {
        let files: &[&Path] = &[];

        let stream = hash_files(files.into_par_iter());
        let result = stream.count();

        assert_eq!(result, 0);

        Ok(())
    }

    #[test]
    fn sequence_with_one_item_returns_one_hash() -> io::Result<()> {
        let expected = vec!["cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323"];

        let (_work_dir, files) = set_up_source_files([("file 1", "file 1 contents")])?;

        let stream = hash_files(files.into_par_iter());
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

        let (_work_dir, files) = set_up_source_files([
            ("file 1", "file 1 contents"),
            ("file 2", "file 2 contents"),
            ("file 3", "more test data"),
        ])?;

        let stream = hash_files(files.into_par_iter());
        let result = stream.collect::<Vec<_>>();
        let result = result
            .into_iter()
            .map(|h| h.expect("file hashed").to_string())
            .collect::<Vec<_>>();

        assert_eq!(result, expected);

        Ok(())
    }

    #[test]
    fn sequence_with_missing_files_continues() -> io::Result<()> {
        let expected = vec![
            Ok("cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323".to_owned()),
            Err(io::ErrorKind::NotFound),
            Ok("7b7e5f11be694b5d2b630bb648b74ce91cee8b56aa773015474a86f451d8f335".to_owned()),
            Ok("9400c17d43042a4546cbb8c251f122888e3ed53403096fde646adcd7370ba21e".to_owned()),
        ];

        let (_work_dir, mut files) = set_up_source_files([
            ("file 1", "file 1 contents"),
            ("file 3", "file 2 contents"),
            ("file 4", "more test data"),
        ])?;

        files.insert(1, "file 2".into());

        let stream = hash_files(files.into_par_iter());
        let result = stream.collect::<Vec<_>>();
        let result = result
            .into_iter()
            .map(|r| r.map(|h| h.to_string()).map_err(|e| e.kind()))
            .collect::<Vec<_>>();

        assert_eq!(result, expected);

        Ok(())
    }
}
