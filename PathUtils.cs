using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsPathsManipulation
{
    internal enum OSFamily
    {
        Windows = 0,
        Linux = 1,
        OSX = 2
    }

    public static class PathUtils
    {
        private static StringComparer _OSDependentPathComparer = (PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        private static StringComparison _OSDependentPathComparisonOption = (PathUtils.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        public static StringComparer OSDependentPathComparer => PathUtils._OSDependentPathComparer;
        public static StringComparison OSDependentPathComparisonOption => PathUtils._OSDependentPathComparisonOption;
        public static bool IsWindows => PathUtils._osFamily == OSFamily.Windows;

        private static OSFamily _osFamily = GetOSFamily();

        private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

        private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        private static OSFamily GetOSFamily()
        {
            return OSFamily.Windows;
        }
        public static bool IsPathValid(string path)
        {
            if (path != null)
            {
                return path.IndexOfAny(PathUtils._invalidPathChars) < 0;
            }
            return false;
        }

        public static bool IsFileNameValid(string fileName)
        {
            if (fileName != null)
            {
                return fileName.IndexOfAny(PathUtils._invalidFileNameChars) < 0;
            }
            return false;
        }

        public static bool ExcludeFileFromAnalysis(string sourceFileFullPath, string[] fileMasks, string[] pathMasks)
        {
            if (string.IsNullOrWhiteSpace(sourceFileFullPath))
            {
                return false;
            }
            string text = PathUtils.NormalizePathMask(sourceFileFullPath);
            string[] array = fileMasks;
            for (int i = 0; i < array.Length; i++)
            {
                string text2 = PathUtils.NormalizePathMask(array[i]);
                if (!string.IsNullOrWhiteSpace(text2))
                {
                    string fileName = Path.GetFileName(sourceFileFullPath);
                    if (!PathUtils.IsMask(text2) && fileName.StartsWith(text2, PathUtils.OSDependentPathComparisonOption))
                    {
                        return true;
                    }
                    if (PathUtils.FitsMask(fileName, text2))
                    {
                        return true;
                    }
                }
            }
            array = pathMasks;
            for (int i = 0; i < array.Length; i++)
            {
                string text3 = PathUtils.NormalizePathMask(array[i]);
                if (!string.IsNullOrEmpty(text3))
                {
                    if (!PathUtils.IsMask(text3) && text.StartsWith(text3, PathUtils.OSDependentPathComparisonOption))
                    {
                        return true;
                    }
                    if (PathUtils.FitsMask(text, "*" + text3 + "*"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string NormalizePathMask(string OriginalPath)
        {
            string empty = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(OriginalPath))
                {
                    return string.Empty;
                }
                string directoryName = Path.GetDirectoryName(OriginalPath);
                string fileName = Path.GetFileName(OriginalPath);
                string extension = Path.GetExtension(OriginalPath);
                if (directoryName == null && string.IsNullOrEmpty(extension) && !string.IsNullOrEmpty(Path.GetPathRoot(OriginalPath)))
                {
                    return Path.GetPathRoot(OriginalPath);
                }
                if (!string.IsNullOrEmpty(directoryName))
                {
                    if ((directoryName.EndsWith("*") || directoryName.EndsWith("?")) && string.IsNullOrEmpty(fileName))
                    {
                        return directoryName;
                    }
                    if (directoryName.Equals(Path.DirectorySeparatorChar.ToString()))
                    {
                        return directoryName + fileName;
                    }
                    return directoryName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + fileName;
                }
                return fileName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static bool IsMask(string fileMask)
        {
            return fileMask.IndexOfAny("?*".ToCharArray()) != -1;
        }

        public static bool FitsMask(string fileName, string fileMask)
        {
            if (string.IsNullOrEmpty(fileMask))
            {
                return false;
            }
            return NativeMethods.PathMatchSpec(fileName, fileMask) != 0;
        }

        public static void FilterOutNonSystemPaths(ref List<string> list, string compilerInstDir, string compilerWorkingDirectory)
        {
            string value = new DirectoryInfo(compilerInstDir).FullName.ToLower();
            string value2 = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%ProgramFiles%")).FullName.ToLower().Replace(" (x86)", "");
            string value3 = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")).FullName.ToLower();
            for (int num = list.Count - 1; num >= 0; num--)
            {
                try
                {
                    string originalPath = list[num].Trim('"');
                    PathUtils.ExpandRelativePath(ref originalPath, compilerWorkingDirectory);
                    originalPath = originalPath.ToLower();
                    if (!originalPath.Contains(value) && !originalPath.Contains(value2) && !originalPath.Contains(value3))
                    {
                        list.RemoveAt(num);
                    }
                }
                catch (Exception)
                {
                    list.RemoveAt(num);
                }
            }
        }

        public static void FilterOutSystemPaths(ref IList<string> list, string compilerInstDir, string compilerWorkingDirectory)
        {
            string value = new DirectoryInfo(compilerInstDir).FullName.ToLower();
            string value2 = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%ProgramFiles%")).FullName.ToLower().Replace(" (x86)", "");
            string value3 = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")).FullName.ToLower();
            for (int num = list.Count - 1; num >= 0; num--)
            {
                try
                {
                    string originalPath = list[num].Trim('"');
                    PathUtils.ExpandRelativePath(ref originalPath, compilerWorkingDirectory);
                    originalPath = originalPath.ToLower();
                    if (originalPath.Contains(value) || originalPath.Contains(value2) || originalPath.Contains(value3))
                    {
                        list.RemoveAt(num);
                    }
                }
                catch (Exception)
                {
                    list.RemoveAt(num);
                }
            }
        }

        public static string ExpandDotsInPathFragment(string pathFragment)
        {
            if (!pathFragment.Contains("." + Path.DirectorySeparatorChar) && !pathFragment.Contains(".." + Path.DirectorySeparatorChar))
            {
                return pathFragment;
            }
            StringBuilder stringBuilder = new StringBuilder(pathFragment.Length);
            for (int i = 0; i < pathFragment.Length; i++)
            {
                if (pathFragment[i] == '.' && ((i > 0 && pathFragment[i - 1] == Path.DirectorySeparatorChar) || i == 0))
                {
                    if (i == pathFragment.Length - 1)
                    {
                        continue;
                    }
                    if (i < pathFragment.Length - 1)
                    {
                        if (pathFragment[i + 1] == Path.DirectorySeparatorChar)
                        {
                            i++;
                            continue;
                        }
                        if (pathFragment[i + 1] == '.' && i < pathFragment.Length - 2 && pathFragment[i + 2] == Path.DirectorySeparatorChar)
                        {
                            i += 2;
                            if (stringBuilder.Length != 0)
                            {
                                int num = stringBuilder.Length - 1;
                                do
                                {
                                    stringBuilder.Remove(num, 1);
                                    num--;
                                }
                                while (num >= 0 && stringBuilder[num] != Path.DirectorySeparatorChar);
                            }
                            continue;
                        }
                    }
                }
                stringBuilder.Append(pathFragment[i]);
            }
            return stringBuilder.ToString();
        }

        public static bool ExpandRelativePath(ref string originalPath, string currentDir)
        {
            try
            {
                string text = originalPath;
                if (!Path.IsPathRooted(text.TrimStart('\\', '/')))
                {
                    text = currentDir + Path.DirectorySeparatorChar + text;
                }
                originalPath = new FileInfo(text).FullName;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ExpandRelativePath(string originalPath, string currentDir)
        {
            if (!PathUtils.ExpandRelativePath(ref originalPath, currentDir))
            {
                return string.Empty;
            }
            return originalPath;
        }

        public static bool ConstructRelativePath(string rootDirectory, string targetPath, out string relativePath)
        {
            relativePath = null;
            if (PathUtils.AreOnTheSameLogicalDrive(rootDirectory, targetPath))
            {
                relativePath = Uri.UnescapeDataString(new Uri(rootDirectory, UriKind.Absolute).MakeRelativeUri(new Uri(targetPath, UriKind.Absolute)).ToString()).Replace('/', Path.DirectorySeparatorChar);
                return true;
            }
            return false;
        }

        private static bool AreOnTheSameLogicalDrive(string firstPath, string secondPath)
        {
            if (!string.IsNullOrWhiteSpace(firstPath) && !string.IsNullOrWhiteSpace(secondPath))
            {
                return string.Equals(Path.GetPathRoot(firstPath), Path.GetPathRoot(secondPath), PathUtils.OSDependentPathComparisonOption);
            }
            return false;
        }
        public static string GetFilenameWithoutCompositeExtension(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.Substring(0, fileName.IndexOf('.'));
        }

        public static string GetCompositeExtension(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.Substring(fileName.IndexOf('.'));
        }

        public static string NormalizePath(string originalPath)
        {
            try
            {
                return new FileInfo(originalPath).FullName;
            }
            catch
            {
                return null;
            }
        }

        public static bool ComparePaths(string path1, string path2)
        {
            string text = PathUtils.NormalizePath(path1);
            string text2 = PathUtils.NormalizePath(path2);
            if (text == null || text2 == null)
            {
                return false;
            }
            return string.Equals(text, text2, PathUtils.OSDependentPathComparisonOption);
        }



        public static bool TextFileExistsAndNotEmpty(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length == 0L)
                {
                    return false;
                }
                if (fileInfo.Length > 2)
                {
                    return true;
                }
                if (string.IsNullOrWhiteSpace(File.ReadAllText(filePath).Trim()))
                {
                    return false;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static bool IsEightDotThreePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || filePath.Length > 260)
            {
                return false;
            }
            int num = filePath.IndexOf('~');
            if (num != -1 && num + 1 < filePath.Length && char.IsDigit(filePath[num + 1]))
            {
                if (num - 6 - 1 >= 0 && filePath[num - 6 - 1] == Path.DirectorySeparatorChar)
                {
                    return filePath.Substring(num - 6, 6).All((char c) => char.IsUpper(c) || char.IsDigit(c) || char.IsWhiteSpace(c) || c == '_');
                }
                return false;
            }
            return false;
        }

        public static string GetCaseSensitivePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }
            if (!File.Exists(filePath))
            {
                return filePath;
            }
            if (filePath.Length >= 2 && char.IsLower(filePath[0]) && filePath[1] == ':')
            {
                filePath = filePath.First().ToString().ToUpper() + filePath.Substring(1);
            }
            if (PathUtils.IsEightDotThreePath(filePath))
            {
                StringBuilder stringBuilder = new StringBuilder(260);
                int longPathName = NativeMethods.GetLongPathName(filePath, stringBuilder, stringBuilder.Capacity);
                if (longPathName > 260)
                {
                    stringBuilder.Capacity = longPathName;
                    longPathName = NativeMethods.GetLongPathName(filePath, stringBuilder, stringBuilder.Capacity);
                }
                if (longPathName > 0)
                {
                    if (char.IsLower(stringBuilder[0]))
                    {
                        stringBuilder[0] = char.ToUpper(stringBuilder[0]);
                    }
                    string text = stringBuilder.ToString(0, longPathName);
                    if (!PathUtils.IsEightDotThreePath(text) && !string.Equals(filePath, text))
                    {
                        return text;
                    }
                }
            }
            try
            {
                string text2 = Path.GetPathRoot(filePath);
                string[] array = filePath.Substring(text2.Length).Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < array.Length; i++)
                {
                    text2 = Directory.GetFileSystemEntries(text2, array[i]).First();
                }
                if (!File.Exists(text2))
                {
                    return filePath;
                }
                return text2;
            }
            catch (Exception)
            {
                return filePath;
            }
        }

        public static string RedirectToTempIfSystemPath(string originalPath)
        {
            if (originalPath.IndexOf(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Path.GetTempPath();
            }
            return originalPath;
        }

        public static string TransformPathToRelative(string absolutePath, string root, bool addSourceTreeRootMarker = true)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || absolutePath.StartsWith("|?|"))
            {
                return absolutePath;
            }
            if (string.IsNullOrWhiteSpace(root))
            {
                return absolutePath;
            }
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                root += Path.DirectorySeparatorChar;
            }
            string text = (addSourceTreeRootMarker ? "|?|" : string.Empty);
            string relativePath;
            if (!PathUtils.ConstructRelativePath(root, absolutePath, out relativePath))
            {
                return absolutePath;
            }
            return text + Path.DirectorySeparatorChar + relativePath;
        }

        public static string TransformPathToAbsolute(string relativePath, string root)
        {
            try
            {
                string text;
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    text = relativePath;
                }
                else if (relativePath.StartsWith("|?|", StringComparison.InvariantCultureIgnoreCase))
                {
                    text = relativePath.Replace("|?|", root.TrimEnd(Path.DirectorySeparatorChar));
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        text = new FileInfo(text).FullName;
                    }
                }
                else
                {
                    text = new FileInfo(relativePath).FullName;
                }
                return text;
            }
            catch (ArgumentException)
            {
                return relativePath?.Replace("|?|", string.Empty);
            }
            return "";
        }
    }
}
