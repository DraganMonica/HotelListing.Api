namespace HotelListing.Api.Common.Results;
// in loc sa arunc Exception, returnez un obiect clar
//                                 Code-identificator-ex: user not found
//                                 Description- mesajul de eroare care se doreste a fi afisat
public readonly record struct Error(string Code, string Description)
{
    public static readonly Error None = new("","");// nu exista eroare
    public bool IsNone=> string.IsNullOrEmpty(Code);
}

public readonly record struct Result
{
    public bool IsSuccess { get; }
    public Error[] Errors { get; }



    private Result(bool isSuccess, Error[] errors)
        => (IsSuccess, Errors) = (isSuccess, errors);

    public static Result Success()
        => new(true, Array.Empty<Error>());
    public static Result NotFound(params Error[] errors)
       => new(false, errors);
    public static Result Failure(params Error[] errors)
        => new(false, errors);
    
    public static Result BadRequest(params Error[] errors)
        => new(false, errors);

    public static Result Combine(params Result[] results)
        => results.Any(r => !r.IsSuccess)
            ? Failure(results.Where(r => !r.IsSuccess)
                             .SelectMany(r => r.Errors)
                             .ToArray())
            : Success();
}

// returneaza  rezultat+status+erori
public readonly record struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error[] Errors { get; }

    private Result(bool isSuccess, T? value, Error[] errors)
        => (IsSuccess, Value, Errors) = (isSuccess, value, errors);

    public static Result<T> Success(T value)
        => new(true, value, Array.Empty<Error>());

    public static Result<T> Failure(params Error[] errors)
        => new(false, default, errors);
    public static Result<T> NotFound()
        => new(false, default, []);
    public static Result<T> BadRequest()
        => new(false, default, []);
    public static Result<T> BadRequest(params Error[] errors)
        => new(false, default, errors);

    // Functional helpers

    public Result<K> Map<K>(Func<T, K> map)
        => IsSuccess
            ? Result<K>.Success(map(Value!))
            : Result<K>.Failure(Errors);

    public Result<K> Bind<K>(Func<T, Result<K>> next)
        => IsSuccess
            ? next(Value!)
            : Result<K>.Failure(Errors);

    public Result<T> Ensure(Func<T, bool> predicate, Error error)
        => IsSuccess && !predicate(Value!)
            ? Failure(error)
            : this;
}
