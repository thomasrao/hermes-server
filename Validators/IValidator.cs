namespace HermesSocketServer.Validators
{
    public interface IValidator
    {
        bool Check(string? input);
    }
}