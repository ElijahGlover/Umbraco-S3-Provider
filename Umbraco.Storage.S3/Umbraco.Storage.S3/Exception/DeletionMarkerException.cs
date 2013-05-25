namespace Umbraco.Storage.S3.Exception
{
    public class DeletionMarkerException : System.Exception
    {
        private readonly string[] _keys;

        public DeletionMarkerException(string key)
        {
            _keys = new [] {key};
        }

        public DeletionMarkerException(string[] keys)
        {
            _keys = keys;
        }

        public override string Message
        {
            get { return string.Format("Could not delete {0} objects with keys {1}", _keys.Length, string.Join(",", _keys)); }
        }
    }
}
