using com.csutil.encryption;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.csutil.http {

    public class Headers : IEnumerable<KeyValuePair<string, string>> {

        private Dictionary<string, string> headers = new Dictionary<string, string>();

        public Headers(Dictionary<string, string> headers) { foreach (var e in headers) { this.headers.Add(e.Key.ToLower(), e.Value); } }

        public string GetFileNameOnServer() {
            var name = ExtractFileName(GetHeaderValue("content-disposition", null));
            if (name.IsNullOrEmpty()) {
                Log.w("Filename not found, will try fallback to hash+mimetype");
                name += GetHeaderValue("Last-Modified", "");
                name += GetHeaderValue("Content-Length", "");
                name += GetContentMimeType("");
                AssertV2.IsFalse(name.IsNullOrEmpty(), "name was emtpy");
                name = name.GetSHA1Hash();
                string ext = GetFileExtensionFromMimeType(null);
                if (ext != null) { name += "." + ext; }
            }
            return name;
        }

        public long GetFileSizeInBytesOnServer() { return long.Parse(GetHeaderValue("Content-Length", "-1")); }

        public override string ToString() {
            if (headers.IsNullOrEmpty()) { return "(Emtpy Headers object)"; }
            var s = "Headers: ((";
            foreach (var e in headers) { s += "\n  > " + e; }
            return s + "/n))";
        }

        public DateTime GetLastModified(DateTime fallbackValue) {
            try {
                string v = GetHeaderValue("last-modified", null);
                if (v == null) { return fallbackValue; }
                return DateTimeParser.NewDateTimeFromUnixTimestamp(long.Parse(v));
            }
            catch (Exception) {
                return fallbackValue;
            }
        }

        public string GetHeaderValue(string headerName, string fallbackValue) {
            return headers.GetValue(headerName.ToLower(), fallbackValue);
        }

        private static string ExtractFileName(string headerWithFilename) {
            if (headerWithFilename == null) { return null; }
            var words = headerWithFilename.Split(';');
            foreach (var word in words) {
                var pair = headerWithFilename.Split('=');
                if (pair != null && pair[0].ToLower().Contains("filename")) { return pair[1].Trim(); }
            }
            return null;
        }

        private string GetFileExtensionFromMimeType(string fallbackValue) {
            var mime = GetContentMimeType(fallbackValue);
            if (mime == null) { return fallbackValue; }
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type
            if (mime.Contains(";")) { mime = mime.Split(";").First(p => p.Contains("/")); }
            var x = mime.Split("/");
            AssertV2.AreEqual(2, x.Length);
            return x.Last();
        }

        public string GetContentMimeType(string fallbackValue) { return GetHeaderValue("Content-Type", fallbackValue); }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() { return headers.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return headers.GetEnumerator(); }
    }

}