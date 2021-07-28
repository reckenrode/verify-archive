// SPDX-License-Identifier: GPL-3.0-only

use blake3::{Hash, Hasher};
use tokio::io::{self, AsyncRead, AsyncReadExt};

pub async fn b3sum(reader: &mut (impl AsyncRead + Unpin)) -> io::Result<Hash> {
    let mut hasher = Hasher::new();
    let mut buffer = [0u8; 16 * 1024];

    let mut bytes_read = reader.read(&mut buffer).await?;
    while bytes_read > 0 {
        hasher.update(&buffer[..bytes_read]);
        bytes_read = reader.read(&mut buffer).await?;
    }

    Ok(hasher.finalize())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[tokio::test]
    async fn checksum_returns_the_b3sum_of_the_input() -> io::Result<()> {
        let expected = "6a953581d60dbebc9749b56d2383277fb02b58d260b4ccf6f119108fa0f1d4ef";
        let mut input: &[u8] = b"test data";
        let result = b3sum(&mut input).await?;
        assert_eq!(result.to_hex().as_str(), expected);
        Ok(())
    }
}
