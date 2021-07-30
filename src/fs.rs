// SPDX-License-Identifier: GPL-3.0-only

use std::path::Path;

use async_stream::stream;
use blake3::Hash;
use futures_core::stream::Stream;
use tokio::{fs::File, io};

use crate::digest::b3sum;

pub fn hash_files(
    paths: impl IntoIterator<Item = impl AsRef<Path>>,
) -> impl Stream<Item = io::Result<Hash>> {
    stream! {
        for ref path in paths {
            match File::open(path).await {
                Ok(mut file) => {
                    let hash = b3sum(&mut file).await?;
                    yield Ok(hash)
                },
                Err(error) => {
                    yield Err(error)
                },
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use std::path::PathBuf;

    use futures_util::{pin_mut, StreamExt};
    use tempfile::{tempdir, TempDir};
    use tokio::io::AsyncWriteExt;

    use super::*;

    async fn set_up_source_files(
        spec: impl IntoIterator<Item = (impl AsRef<Path>, impl AsRef<str>)>,
    ) -> io::Result<(TempDir, Vec<PathBuf>)> {
        let dir = tempdir()?;
        let mut paths = Vec::new();
        for (file, contents) in spec {
            let path = dir.path().join(file);
            let mut file = File::create(&path).await?;
            paths.push(path);
            file.write(contents.as_ref().as_bytes()).await?;
        }
        Ok((dir, paths))
    }

    #[tokio::test]
    async fn empty_sequence_returns_no_hashes() -> io::Result<()> {
        let files: &[&Path] = &[];

        let stream = hash_files(files);
        pin_mut!(stream);

        let result = stream.next().await;

        assert_eq!(result.is_none(), true);

        Ok(())
    }

    #[tokio::test]
    async fn sequence_with_one_item_returns_one_hash() -> io::Result<()> {
        let expected = "cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323";

        let (_work_dir, files) = set_up_source_files([("file 1", "file 1 contents")]).await?;

        let stream = hash_files(files);
        pin_mut!(stream);

        let result = stream
            .next()
            .await
            .map(|o| o.expect("file hashed").to_string());
        let next = stream.next().await;

        assert_eq!(result.expect("is some"), expected);
        assert_eq!(next.is_none(), true);

        Ok(())
    }

    #[tokio::test]
    async fn sequence_with_multiple_items_returns_hashes_in_order() -> io::Result<()> {
        let expected = vec![
            "cef558d2715440bed7e29eef9b8e798cbf0f165cf201c493c14d746659688323",
            "7b7e5f11be694b5d2b630bb648b74ce91cee8b56aa773015474a86f451d8f335",
            "9400c17d43042a4546cbb8c251f122888e3ed53403096fde646adcd7370ba21e",
        ];

        let (_work_dir, files) = set_up_source_files([
            ("file 1", "file 1 contents"),
            ("file 2", "file 2 contents"),
            ("file 3", "more test data"),
        ])
        .await?;

        let stream = hash_files(files);
        pin_mut!(stream);

        let result = stream.collect::<Vec<_>>().await;
        let result = result
            .into_iter()
            .map(|h| h.expect("file hashed").to_string())
            .collect::<Vec<_>>();

        assert_eq!(result, expected);

        Ok(())
    }

    #[tokio::test]
    async fn sequence_with_missing_files_continues() -> io::Result<()> {
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
        ])
        .await?;

        files.insert(1, "file 2".into());

        let stream = hash_files(files);
        pin_mut!(stream);

        let result = stream.collect::<Vec<_>>().await;
        let result = result
            .into_iter()
            .map(|r| r.map(|h| h.to_string()).map_err(|e| e.kind()))
            .collect::<Vec<_>>();

        assert_eq!(result, expected);

        Ok(())
    }
}
