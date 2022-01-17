using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendFileToFtp
{
    public abstract class Result<T>
    {
        public bool Success { get; protected set; }
        public bool Failure => !Success;

        public T Data
        {
            get => Success ? _data : throw new Exception("Operation was not successful");
            set => _data = value;
        }

        public Exception Exception
        {
            get => Success ? throw new Exception("Operation was successful") : _exception;
            set => _exception = value;
        }

        protected T? _data;
        protected Exception? _exception;

    }

    public class Success<T> : Result<T>
    {
        public Success(T data) {
            _data = data;
            Success = true;
        }
    }

    public class Failure <T> : Result <T>
    {
        public Failure (Exception exception)
        {
            _exception = exception;
            Success = false;
        }
    }
}
