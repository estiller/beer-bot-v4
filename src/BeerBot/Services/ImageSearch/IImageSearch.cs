using System;
using System.Threading.Tasks;

namespace BeerBot.Services.ImageSearch
{
    public interface IImageSearch
    {
        Task<Uri> SearchImage(string query);
    }
}