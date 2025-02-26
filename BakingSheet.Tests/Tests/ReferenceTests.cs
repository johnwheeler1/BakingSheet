// BakingSheet, Maxwell Keonwoo Kang <code.athei@gmail.com>, 2022

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cathei.BakingSheet.Tests
{
    public class ReferenceTests : IDisposable
    {
        private TestLogger _logger;
        private TestSheetContainer _container;

        public ReferenceTests()
        {
            _logger = new TestLogger();
            _container = new TestSheetContainer(_logger);
        }

        public void Dispose()
        {

        }

        [Fact]
        public void TestReference()
        {
            _container.Tests = new TestSheet();
            _container.Refers = new TestReferenceSheet();

            var referRow = new TestReferenceSheet.Row();

            referRow.Id = "Refer";
            referRow.ReferColumn = new TestSheet.Reference("Test");

            _container.Refers.Add(referRow);

            var testRow = new TestSheet.Row();

            testRow.Id = "Test";

            _container.Tests.Add(testRow);

            _container.PostLoad();
            _container.Verify();

            _logger.VerifyNoError();

            Assert.Equal(testRow, _container.Refers["Refer"].ReferColumn.Ref);
        }

        [Fact]
        public void TestNestedReference()
        {
            _container.Tests = new TestSheet();
            _container.Refers = new TestReferenceSheet();

            var referRow = new TestReferenceSheet.Row();

            referRow.Id = "Refer";
            referRow.ReferColumn = new TestSheet.Reference("Test1");

            _container.Refers.Add(referRow);

            var referElem1 = new TestReferenceSheet.Elem();

            referElem1.NestedReferColumn = new TestSheet.Reference("Test2");

            var referElem2 = new TestReferenceSheet.Elem();

            referElem2.NestedReferColumn = new TestSheet.Reference("Test1");

            referRow.Arr.Add(referElem1);
            referRow.Arr.Add(referElem2);

            var testRow1 = new TestSheet.Row();

            testRow1.Id = "Test1";
            testRow1.Content = "Content1";

            _container.Tests.Add(testRow1);

            var testRow2 = new TestSheet.Row();

            testRow2.Id = "Test2";
            testRow2.Content = "Content2";

            _container.Tests.Add(testRow2);

            _container.PostLoad();
            _container.Verify();

            _logger.VerifyNoError();

            Assert.Equal("Content1", _container.Refers["Refer"].ReferColumn.Ref.Content);
            Assert.Equal("Content2", _container.Refers["Refer"][0].NestedReferColumn.Ref.Content);
            Assert.Equal("Content1", _container.Refers["Refer"][1].NestedReferColumn.Ref.Content);
        }

        [Fact]
        public void TestNullReference()
        {
            _container.Tests = new TestSheet();
            _container.Refers = new TestReferenceSheet();

            var referRow = new TestReferenceSheet.Row();

            referRow.Id = "Refer";
            referRow.ReferColumn = new TestSheet.Reference(null);

            _container.Refers.Add(referRow);

            var testRow = new TestSheet.Row();

            testRow.Id = "Test";

            _container.Tests.Add(testRow);

            _container.PostLoad();
            _container.Verify();

            _logger.VerifyNoError();

            Assert.Null(_container.Refers["Refer"].ReferColumn.Ref);
        }

        [Fact]
        public void TestWrongReference()
        {
            _container.Tests = new TestSheet();
            _container.Refers = new TestReferenceSheet();

            var referRow = new TestReferenceSheet.Row();

            referRow.Id = "Refer";
            referRow.ReferColumn = new TestSheet.Reference("WrongId");

            _container.Refers.Add(referRow);

            var testRow = new TestSheet.Row();

            testRow.Id = "Test";

            _container.Tests.Add(testRow);

            _container.PostLoad();
            _container.Verify();

            Assert.Equal("WrongId", _container.Refers["Refer"].ReferColumn.Id);
            Assert.Null(_container.Refers["Refer"].ReferColumn.Ref);
        }

        [Fact]
        public void TestSelfReference()
        {
            _container.Refers = new TestReferenceSheet();

            var referRow = new TestReferenceSheet.Row();

            referRow.Id = "Refer";
            referRow.SelfReferColumn = new TestReferenceSheet.Reference("Refer");

            _container.Refers.Add(referRow);

            _container.PostLoad();
            _container.Verify();

            _logger.VerifyNoError();

            Assert.Equal("Refer", _container.Refers["Refer"].SelfReferColumn.Id);
            Assert.Equal(_container.Refers["Refer"], _container.Refers["Refer"].SelfReferColumn.Ref);
        }
    }
}
