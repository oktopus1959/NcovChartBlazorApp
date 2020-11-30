using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace StandardCommon
{
    public class CsvReader : IDisposable
    {
        public string FilePath { get; set; }

        private FileStream m_fs = null;
        private StreamReader m_reader = null;

        private bool[] m_numberColumns;

        public static bool[] ParseHeaders(IEnumerable<string> headers)
        {
            return headers?.Select(x => x._safeSubstring(-2) == "/N").ToArray();
        }

        public CsvReader(string path)
        {
            FilePath = path;
            m_fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            m_reader = new System.IO.StreamReader(m_fs);
        }

        ~CsvReader()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
        }

        public void ReadHeader()
        {
            m_numberColumns = ParseHeaders(ReadItems());
        }

        public void SetHeaders(string[] headers)
        {
            m_numberColumns = ParseHeaders(headers);
        }

        /// <summary>
        /// 一行読み込んで各アイテムを split した配列を返す。ファイルの終わりに達したら null を返す。
        /// </summary>
        /// <returns></returns>
        public string[] ReadItems()
        {
            List<string> results = new List<string>();
            string item = "";
            bool inDq = false;
            string line = null;
            while ((line = m_reader.ReadLine()) != null)
            {
                line = line._reReplace(@"\r*\n$", "");
                int i = 0;
                if (inDq)
                {
                    int cm = findNextComma(line, 0, true);
                    if (cm < 0)
                    {
                        item += line + "\n";
                        continue;
                    }
                    results.Add(item + line._safeSubstring(0, cm));
                    inDq = false;
                    i = cm + 1;
                }
                while (i < line.Length)
                {
                    i = skipBlank(line, i);
                    if (i < 0) break;
                    int cm = findNextComma(line, i, false);
                    if (cm < 0)
                    {
                        inDq = true;
                        item = line._safeSubstring(i) + "\n";
                        i = -1;
                        break;
                    }
                    results.Add(line._safeSubstring(i, cm - i));
                    i = cm + 1;
                }
                if (i == line.Length)
                {
                    results.Add("");
                }
                if (!inDq) break;
            }
            return line != null ? results.Select(x => removeDq(x)).ToArray() : null;
        }

        private int skipBlank(string line, int start)
        {
            while (start < line.Length)
            {
                var ch = line[start];
                // 空白をスキップ
                if (ch != ' ' && ch != '\t') break;
                ++start;
            }
            return start;
        }

        /// <summary>
        /// 次のカンマ位置を返す。ダブルクォートの中なら -1 を返す。EOL なら末尾位置を返す。
        /// </summary>
        /// <param name="line"></param>
        /// <param name="start"></param>
        /// <param name="inDq"></param>
        /// <returns></returns>
        private int findNextComma(string line, int start, bool inDq)
        {
            if (inDq)
            {
                int dq = findNextDoubleQuote(line, start);
                if (dq < 0) return -1;
                start = dq + 1;
            }
            start = skipBlank(line, start);
            if (start >= line.Length) return line.Length;

            var ch = line[start];
            if (ch == '"')
            {
                return findNextComma(line, start + 1, true);
            }
            else
            {
                int cm = line._safeIndexOf(',', start);
                return cm >= 0 ? cm : line.Length;
            }
        }

        private int findNextDoubleQuote(string line, int start)
        {
            while (true)
            {
                int dq = line._safeIndexOf('"', start);
                if (dq < 0)
                    return -1;
                if (dq >= line.Length - 1 || line[dq + 1] != '"')
                    return dq;
                start = dq + 2;    // "" が見つかった
            }
        }

        public void Close()
        {
            if (m_reader != null)
            {
                m_reader.Close();
                m_reader = null;
            }
            if (m_fs != null)
            {
                m_fs.Close();
                m_fs = null;
            }
        }

        /// <summary>
        /// 文字列を囲むダブルクォートを除去
        /// </summary>
        private string removeDq(string item)
        {
            if (item._isEmpty()) return "";

            if (item.Length >= 2 && item[0] == '"' && item.Last() == '"')
            {
                // 両端がダブルクォートで囲まれている
                item = item._safeSubstring(1, item.Length - 2).Replace("\"\"", "\"");
            }
            if (item.Length >= 2 && item[0] == '"' && item.Last() == '"')
            {
                // さらに両端がダブルクォートで囲まれている
                item = item._safeSubstring(1, item.Length - 2);
            }
            else if (item.Length > 0 && item[0] == '\'')
            {
                // 先頭がシングルクォートなら、それを除去する
                item = item._safeSubstring(1);
            }
            return item.Replace(@"\n", "\n");
        }

        /// <summary>
        /// CSVファイルからデータを読み込む<para/>
        /// エラーの場合は、{ null, "エラーメッセージ" } を要素とするリストを返す。
        /// </summary>
        public static List<string[]> RestoreTable(string csvFilePath)
        {
            var result = new List<string[]>();
            try
            {
                using (var csvReader = new CsvReader(csvFilePath))
                {
                    string[] items = null;
                    while ((items = csvReader.ReadItems()) != null)
                    {
                        result.Add(items);
                    }
                }
            }
            catch (Exception e)
            {
                result.Clear();
                result.Add(new string[] { null, e._getErrorMsg() });
            }
            return result;
        }

    }

    public class CsvWriter : IDisposable
    {
        public string FilePath { get; set; }

        public bool IsRealNewLine { get; set; } = false;

        private StreamWriter m_writer = null;

        private bool[] m_numberColumns;

        public CsvWriter(string path, bool bReadNewLine = false)
        {
            FilePath = path;
            IsRealNewLine = bReadNewLine;
            m_writer = new System.IO.StreamWriter(path, false);
        }

        ~CsvWriter()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// ヘッダーの解析を行う。bWrite == true の場合は同時にファイルにも書き込む。
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="bWrite"></param>
        public void SetHeaders(string[] headers, bool bWrite = true)
        {
            m_numberColumns = CsvReader.ParseHeaders(headers);
            if (bWrite) WriteItems(headers);
        }

        public void WriteItems(string[] items)
        {
            var numColumns = m_numberColumns;
            if (numColumns.Length < items.Length)
            {
                numColumns = numColumns.Concat(new bool[items.Length - numColumns.Length]).ToArray();
            }
            var line = items.Zip(numColumns, (x, y) => encloseWithDq(x, y))._join(",");
            m_writer.WriteLine(line);
        }

        public void Close()
        {
            if (m_writer != null)
            {
                try
                {
                    m_writer.Close();
                }
                catch (Exception)
                {
                }
                m_writer = null;
            }
        }

        /// <summary>
        /// 文字列をダブルクォートで囲む
        /// </summary>
        private string encloseWithDq(string item, bool isNum)
        {
            if (item == null) item = "";
            item = item.Replace("\\r", "").Replace("\r", "");
            item = IsRealNewLine ? item.Replace(@"\n", "\n") : item.Replace("\n", @"\n");
            if (!isNum && item._notEmpty() &&
                !item._reMatch(@"^ +$") &&
                (item.StartsWith(" ") || item.EndsWith(" ") || item._reMatch(@"^[=\+\-\']") || item._reMatch(@"^[0-9()+\-*/@$%.:=\\ ]+$") || item.Contains('"') || item.Contains('\n')))
            {
                // ダブルクォートで囲む必要あり (シングルクォートの場合、たとえば「'12」をExcelで保存すると「12」となってしまうため)
                item = "\"" + item + "\"";
            }

            if (item.IndexOf('"') > -1)
            {
                //"を""とする
                item = item.Replace("\"", "\"\"");
            }
            return "\"" + item + "\"";
        }

        /// <summary>
        /// テーブルをCSVファイルにダンプする<para/>
        /// エラーメッセージを返す。
        /// </summary>
        public static string DumpTable(string csvFilePath, string[] headers, IEnumerable<string[]> data)
        {
            try
            {
                var dirPath = Path.GetDirectoryName(csvFilePath);
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                using (var csvWriter = new CsvWriter(csvFilePath))
                {
                    if (headers._notEmpty())
                    {
                        // ヘッダー(数値カラムを調べておく)
                        csvWriter.SetHeaders(headers, false);
                    }

                    foreach (var line in data)
                    {
                        csvWriter.WriteItems(line);
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                return e._getErrorMsg();
            }
        }

    }

}
