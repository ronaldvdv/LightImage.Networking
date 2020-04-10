namespace LightImage.FileSharing.IO
{
    /// <summary>
    /// Writer for file chunks
    /// </summary>
    public interface IChunkWriter
    {
        /// <summary>
        /// Finalize a file
        /// </summary>
        /// <param name="path">Path to the file that should be closed</param>
        void Close(string path);

        /// <summary>
        /// Write a chunk of data to a file
        /// </summary>
        /// <param name="path">Path to the file that should be written</param>
        /// <param name="offset">Offset of the first byte to be written</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="data">Actual bytes to be written</param>
        /// <exception cref="IOException">Thrown if the file cannot be found or written</exception>
        void Write(string path, long offset, int count, byte[] data);
    }
}