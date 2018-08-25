using System;
using System.Threading.Tasks;

namespace BeerBot.Services
{
    public interface IImageSearchService
    {
        Task<Uri> SearchImage(string query);
    }
}