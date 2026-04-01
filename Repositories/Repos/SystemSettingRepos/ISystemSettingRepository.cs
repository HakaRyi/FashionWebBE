namespace Repositories.Repos.SystemSettingRepos
{
    public interface ISystemSettingRepository
    {
        Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue);
        Task<int> GetIntValueAsync(string key, int defaultValue);
        Task<string> GetStringValueAsync(string key, string defaultValue);
        Task<double> GetDoubleValueAsync(string key, double defaultValue);
    }
}
