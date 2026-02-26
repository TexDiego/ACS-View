using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.UseCases.DTOs;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class DashboardMetricsService(IDatabaseService db) : IDashboardMetricsService
    {
        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task<DashboardMetricsDto> GetMetricsAsync()
        {
            var metrics = new DashboardMetricsDto()
            {
                TotalPacientes = await _connection.Table<Patient>().CountAsync(),
                //...
                //...
                //...
            };
            return metrics;
        }
    }
}