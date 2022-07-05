using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBC.OpenAPI.SDK.Core.Models;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Web;
using System.Text.Encodings.Web;

namespace TBC.OpenAPI.SDK.Core.Tests
{
    public class ComponentTests
    {
        [Theory]
        [InlineData("name", "Shota Rustaveli")]
        [InlineData("name", "Shota&Rustaveli")]
        [InlineData("name", "Shota?Rustaveli")]
        [InlineData("name?", "ShotaRustaveli")]
        [InlineData("name&", "ShotaRustaveli")]
        [InlineData("name!", "ShotaRustaveli")]
        [InlineData("(name)", "ShotaRustaveli")]
        [InlineData("name", "Shota! Rustaveli")]
        [InlineData("name", "Shota.Rustaveli")]
        [InlineData("name", "Shota_Rustaveli")]
        [InlineData("name", "Shota-Rustaveli")]
        [InlineData("name", "Shota(Rustaveli)")]
        [InlineData("name", "შოთა რუსთაველი")]
        public void QueryParamCollection_ToQueryString_UrlEncode_RespondShouldBePositive(string key, string value)
        {
            //Arrange
            QueryParamCollection col = new QueryParamCollection();
            col.Add("paramKey", "paramValue");
            col.Add(key, value);
            
            //act
            var result = col.ToQueryString();

            //assert
            result.Should().NotBeNullOrEmpty();
            var shouldBe = $"?paramKey=paramValue&{UrlEncoder.Default.Encode(key)}={UrlEncoder.Default.Encode(value)}";
            result.Should().Be(shouldBe);
        }

        [Theory]
        [InlineData("", "Shota Rustaveli")]
        [InlineData("Name", null)]
        [InlineData("Name", "")]
        [InlineData("", "")]
        public void QueryParamCollection_ToQueryString_IfHaveNullKeyOrValue_PositiveCases(string key, string value)
        {
            //Arrange
            QueryParamCollection col = new QueryParamCollection();
            col.Add(key, value);

            //act
            var result = col.ToQueryString();

            //assert
            var shouldBe = "";
            result.Should().Be(shouldBe);
        }

        [Theory]
        [InlineData(null, "Shota Rustaveli")]
        [InlineData(null, null)]
        public void QueryParamCollection_ToQueryString_IfHaveNullKeyOrValue_ThrowException(string key, string value)
        {
            //Arrange
            QueryParamCollection col = new QueryParamCollection();

            //act
            Action result = ()=> col.Add(key, value);

            //assert
            result.Should().Throw<ArgumentNullException>();
        }
    }
}
