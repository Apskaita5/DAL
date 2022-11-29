using A5Soft.DAL.Core;
using A5Soft.DAL.MySql;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace A5Soft.DAL.Test
{
    public class ConnectionCloseTest
    {
        private readonly string _connString;
        private readonly IOrmService _service;
        private readonly Random _random = new Random();

        public ConnectionCloseTest()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ConnectionCloseTest>()
                .Build();

            _connString = configuration["ConnString"];
            var agent = new MySqlAgent(_connString, "test_dal_db", null);
            _service = agent.GetDefaultOrmService(null);
        }


        [Fact]
        public async void TestSimple()
        {
            for (int i = 0; i < 1000; i++)
            {
                var obj = new TestEntitySimple()
                {
                    IntValue = _random.Next(0, 100000),
                    StringValue = $"test{_random.Next(0, 100000)}"
                };

                await _service.ExecuteInsertAsync(obj);
                _ = await _service.FetchEntityAsync<TestEntitySimple>(obj.Id);
                obj.StringValue = $"test{_random.Next(0, 100000)}";
                await _service.ExecuteUpdateAsync(obj);
                await _service.ExecuteDeleteAsync<TestEntitySimple>(obj.Id);
            }
        }

        [Fact]
        public async void TestTransaction()
        {
            var obj = new TestEntitySimple()
            {
                IntValue = _random.Next(0, 100000),
                StringValue = $"test{_random.Next(0, 100000)}"
            };

            await _service.ExecuteInsertAsync(obj);

            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    await _service.Agent.ExecuteInTransactionAsync(async () =>
                            {
                                obj.StringValue = $"test{_random.Next(0, 100000)}";
                                await _service.ExecuteUpdateAsync(obj);
                                if (_random.Next(0, 100000) < 10000) throw new ApplicationException();
                            });
                }
                catch (ApplicationException)
                { // this is expected to test rollback
                }
            }

            await _service.ExecuteDeleteAsync<TestEntitySimple>(obj.Id);
        }
    }
}
