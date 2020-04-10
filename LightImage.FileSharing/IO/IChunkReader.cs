using System.IO;

namespace LightImage.FileSharing.IO
{
    /// <summary>
    /// Reader for file chunks
    /// </summary>
    public interface IChunkReader
    {
        /// <summary>
        /// Read a chunk of data from a file
        /// </summary>
        /// <param name="path">Path to the file that should be read</param>
        /// <param name="offset">Offset of the first byte within the file</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="buffer">Buffer into which data shall be placed</param>
        /// <returns>Amount of data read from the file</returns>
        /// <exception cref="IOException">Thrown if the file cannot be found or read</exception>
        int Read(string path, long offset, int count, byte[] buffer);
    }
}