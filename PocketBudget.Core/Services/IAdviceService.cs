namespace PocketBudget.Services;

public interface IAdviceService
{
    Task<string> GetRandomAdviceAsync();
}