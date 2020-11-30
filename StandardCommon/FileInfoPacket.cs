using System;
using System.IO;

namespace StandardCommon
{
    /// <summary>
    /// ファイル情報(名前、更新日、サイズ)を格納するパケットクラス
    /// </summary>
    public class FileInfoPacket
    {
        /// <summary>ファイルを参照するための名称</summary>
        public string Name = "";

        /// <summary>ルートディレクトリのパス</summary>
        public string RootPath = "";

        /// <summary>ファイルのロングパス(ルートからの相対パス; フォルダ部も含むもの)</summary>
        public string LongFilePath = "";

        /// <summary>ファイルサイズ(バイト数)</summary>
        public long Size = 0;

        /// <summary>更新日時</summary>
        public DateTime ModifyDt;

    }
    public static partial class Helper
    {
        /// <summary>
        /// ファイルの情報を取得する
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static FileInfoPacket GetFileInfo(string filePath, string rootPath = "")
        {
            try {
                var fileInfo = new FileInfo(filePath);
                rootPath = rootPath._toSafe();
                return new FileInfoPacket() {
                    RootPath = rootPath,
                    LongFilePath = GetRelativePart(rootPath, filePath),
                    Size = fileInfo.Length,
                    ModifyDt = fileInfo.LastWriteTime,
                };
            } catch (Exception) {
                return new FileInfoPacket() {
                    LongFilePath = filePath,
                    Size = -1,
                    ModifyDt = DateTime.MinValue,
                };
            }
        }

        public static long GetFileSize(string filePath)
        {
            return GetFileInfo(filePath)?.Size ?? -1;
        }
    }
}
