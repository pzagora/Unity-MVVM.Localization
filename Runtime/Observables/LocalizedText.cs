using System;
using System.Collections.Generic;
using System.Linq;
using Commons;

namespace MVVM.Localization
{
    public class LocalizedText : BaseObservable
    {
        private string _key;
        private object[] _args = Array.Empty<object>();

        public LocalizedText() : this(string.Empty) { }
        public LocalizedText(string key, params object[] args) 
            => Set(key, args);

        public string Key
        {
            get => _key; 
            set => UpdateValue(ref _key, value);
        }

        public object[] Args
        {
            get => _args; 
            set => UpdateValue(ref _args, GetNormalizedArgs(value));
        }

        public LocalizedText Set(string key, params object[] args)
        {
            Lock();
            
            SetKey(key);
            SetArgs(args);
            
            Unlock();
            
            return this;
        }

        public LocalizedText SetKey(string key)
        {
            Key = key;
            return this;
        }
        
        public LocalizedText SetArgs(params object[] args)
        {
            Args = args ?? Array.Empty<object>();
            return this;
        }

        private static object[] GetNormalizedArgs(IReadOnlyCollection<object> src)
        {
            if (src == null || src.Count == 0) 
                return Array.Empty<object>();
            
            return src.ToArray();
        }
    }
}