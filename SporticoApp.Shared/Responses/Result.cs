using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Responses
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public static Result Success(string? message = null) 
            => new Result { IsSuccess = true, Message = message };
        public static Result Fail(string? message = null)
            => new Result { IsSuccess = false, Message = message };
    }

    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public static Result<T> Success(T data)
            => new Result<T> { IsSuccess = true, Data = data };

        public static Result<T> Fail(string message)
            => new Result<T> { IsSuccess = false, Message = message };
    }

}
