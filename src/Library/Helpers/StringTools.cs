namespace Library.Helpers
{
    public static class StringTools
    {
        public static string GetReadableFileSize(UInt64 fileSize)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];

            var order = 0;
            while (fileSize >= 1024 && order < sizes.Length - 1)
            {
                order++;
                fileSize = fileSize / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            var result = $"{fileSize:0.##} {sizes[order]}";
            return result;
        }
    }
}
