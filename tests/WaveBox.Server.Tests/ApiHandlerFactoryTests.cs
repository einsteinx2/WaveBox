using System;
using WaveBox.ApiHandler;
using WaveBox.ApiHandler.Handlers;
using Xunit;

namespace WaveBox.Server.Tests {
    public class ApiHandlerFactoryTests {
        private static ApiHandlerFactory Factory() {
            ApiHandlerFactory factory = new ApiHandlerFactory();
            factory.Initialize();
            return factory;
        }

        [Fact]
        public void CreateApiHandlerReturnsRegisteredHandlerByName() {
            ApiHandlerFactory factory = Factory();

            Assert.IsType<AlbumsApiHandler>(factory.CreateApiHandler("albums"));
            Assert.IsType<StatusApiHandler>(factory.CreateApiHandler("status"));
        }

        [Fact]
        public void CreateApiHandlerReturnsNullForUnknownName() {
            Assert.Null(Factory().CreateApiHandler("nope"));
        }

        [Fact]
        public void CreateApiHandlerNameLookupIsCaseSensitive() {
            Assert.Null(Factory().CreateApiHandler("Albums"));
        }
    }
}
