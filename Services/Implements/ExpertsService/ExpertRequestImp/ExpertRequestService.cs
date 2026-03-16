using Repositories.Repos.ExpertRequestRepos;
using Services.Response.AccountRep;

namespace Services.Implements.ExpertsService.ExpertRequestImp
{
    public class ExpertRequestService : IExpertRequestService
    {
        private readonly IExpertRequestRepository expertFileRepository;
        public ExpertRequestService(IExpertRequestRepository expertFileRepository)
        {
            this.expertFileRepository = expertFileRepository;
        }

        public async Task<ExpertFileResponse> GetById(int id)
        {
            var expertFile = await expertFileRepository.GetById(id);
            if (expertFile == null)
            {
                return null;
            }
            var response = new ExpertFileResponse
            {
                ExpertFileId = expertFile.ExpertFileId,
                ExpertProfileId = expertFile.ExpertProfileId,
                CertificateUrl = expertFile.CertificateUrl,
                LicenseUrl = expertFile.LicenseUrl,
                //Bio = expertFile.Bio,
                //RatingAvg = expertFile.RatingAvg,
                //ExperienceYears = expertFile.ExperienceYears,
                //Verified = expertFile.Verified,
                Status = expertFile.Status,
                CreatedAt = expertFile.CreatedAt
            };
            return response;
        }
    }
}
