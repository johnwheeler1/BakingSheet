// BakingSheet, Maxwell Keonwoo Kang <code.athei@gmail.com>, 2022

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cathei.BakingSheet.Tests
{
    public class CsvExportTests
    {
        private TestFileSystem _fileSystem;
        private TestLogger _logger;
        private TestSheetContainer _container;
        private CsvSheetConverter _converter;

        public CsvExportTests()
        {
            _fileSystem = new TestFileSystem();
            _logger = new TestLogger();
            _container = new TestSheetContainer(_logger);
            _converter = new CsvSheetConverter("testdata", TimeZoneInfo.Utc, fileSystem: _fileSystem);
        }

        [Fact]
        public async Task TestExportEmptyCsv()
        {
            _container.Tests = new TestSheet();
            _container.Arrays = new TestArraySheet();

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            Assert.True(result);

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Tests.csv"), "Id,Content\n");
            _fileSystem.VerifyTestData(Path.Combine("testdata", "Arrays.csv"), "Id,Content,ElemContent\n");
        }

        [Fact]
        public async Task TestExportSampleCsv()
        {
            _container.Tests = new TestSheet();
 
            var testRow = new TestSheet.Row {
                Id = "TestId",
                Content = "TestContent"
            };

            _container.Tests.Add(testRow);

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Tests.csv"), "Id,Content\nTestId,TestContent\n");
        }

        [Fact]
        public async Task TestExportArrayCsv()
        {
            _container.Arrays = new TestArraySheet();

            var arrayRow = new TestArraySheet.Row {
                Id = "TestId",
                Content = "TestContent"
            };

            var arrayElem1 = new TestArraySheet.Elem {
                ElemContent = "TestElemContent1"
            };

            var arrayElem2 = new TestArraySheet.Elem {
                ElemContent = "TestElemContent2"
            };

            arrayRow.Arr.Add(arrayElem1);
            arrayRow.Arr.Add(arrayElem2);

            _container.Arrays.Add(arrayRow);

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Arrays.csv"), "Id,Content,ElemContent\nTestId,TestContent,TestElemContent1\n,,TestElemContent2\n");
        }

        [Fact]
        public async Task TestExportNestedCsv()
        {
            _container.Nested = new TestNestedSheet();

            var row1 = new TestNestedSheet.Row {
                Id = "Row1",
                StructList = new List<TestNestedSheet.NestedStruct>()
            };

            var row2 = new TestNestedSheet.Row {
                Id = "Row2",
                Struct = new TestNestedSheet.NestedStruct {
                    XInt = 10, YFloat = 50.42f, ZList = new List<string> { "x", "y" }
                },
                StructList = null
            };

            var row3 = new TestNestedSheet.Row {
                Id = "Row3",
                StructList = new List<TestNestedSheet.NestedStruct> {
                    new TestNestedSheet.NestedStruct {
                        XInt = 1, YFloat = 0.124f, ZList = new List<string> { "a", "b" }
                    },
                    new TestNestedSheet.NestedStruct {
                        XInt = 2, YFloat = 20, ZList = new List<string> { "c" }
                    }
                }
            };

            var elem1 = new TestNestedSheet.Elem {
                IntList = new List<int> { 1, 2, 3 }
            };

            var elem2 = new TestNestedSheet.Elem {
                IntList = new List<int> { 4, 5, 6, 7, 8 }
            };

            var elem3 = new TestNestedSheet.Elem {
                IntList = null
            };

            row1.Arr.Add(elem1);
            row1.Arr.Add(elem2);

            row3.Arr.Add(elem3);

            _container.Nested.Add(row1);
            _container.Nested.Add(row2);
            _container.Nested.Add(row3);

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Nested.csv"), "Id,Struct:XInt,Struct:YFloat,Struct:ZList:1,Struct:ZList:2,StructList:1:XInt,StructList:1:YFloat,StructList:1:ZList:1,StructList:1:ZList:2,StructList:2:XInt,StructList:2:YFloat,StructList:2:ZList:1,StructList:2:ZList:2,IntList:1,IntList:2,IntList:3,IntList:4,IntList:5\nRow1,0,0,,,,,,,,,,,1,2,3,,\n,,,,,,,,,,,,,4,5,6,7,8\nRow2,10,50.42,x,y,,,,,,,,,,,,,\nRow3,0,0,,,1,0.124,a,b,2,20,c,,,,,,\n");
        }

        [Fact]
        public async Task TestExportDictCsv()
        {
            _container.Dict = new TestDictSheet();

            var row1 = new TestDictSheet.Row {
                Id = "Dict1",
                Dict = new Dictionary<string, float> {
                    { "A", 10.0f }, { "B", 20.0f }
                }
            };

            var row2 = new TestDictSheet.Row {
                Id = "Dict2",
                Dict = new Dictionary<string, float> {
                    { "C", 10.0f }, { "B", 20.0f }
                }
            };

            var row3 = new TestDictSheet.Row {
                Id = "Empty",
            };

            var elem1 = new TestDictSheet.Elem {
                NestedDict = new Dictionary<int, List<string>> {
                    { 2034, new List<string> { "X", "YYY", "ZZZZZ" } }
                }
            };

            var elem2 = new TestDictSheet.Elem {
                Value = 8
            };

            var elem3 = new TestDictSheet.Elem {
                Value = 65
            };

            row1.Arr.Add(elem1);

            row3.Arr.Add(elem2);
            row3.Arr.Add(elem3);

            _container.Dict.Add(row1);
            _container.Dict.Add(row2);
            _container.Dict.Add(row3);

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Dict.csv"), "Id,Dict:A,Dict:B,Dict:C,NestedDict:2034:1,NestedDict:2034:2,NestedDict:2034:3,Value\nDict1,10,20,,X,YYY,ZZZZZ,0\nDict2,,20,10,,,,\nEmpty,,,,,,,8\n,,,,,,,65\n");
        }

        [Fact]
        public async Task TestExportDictSplitCsv()
        {
            _converter.SplitHeader = true;

            _container.Dict = new TestDictSheet();

            var row1 = new TestDictSheet.Row {
                Id = "Dict1",
                Dict = new Dictionary<string, float> {
                    { "A", 10.0f }, { "B", 20.0f }
                }
            };

            var row2 = new TestDictSheet.Row {
                Id = "Dict2",
                Dict = new Dictionary<string, float> {
                    { "C", 10.0f }, { "B", 20.0f }
                }
            };

            var row3 = new TestDictSheet.Row {
                Id = "Empty",
            };

            var elem1 = new TestDictSheet.Elem {
                NestedDict = new Dictionary<int, List<string>> {
                    { 2034, new List<string> { "X", "YYY", "ZZZZZ" } }
                }
            };

            var elem2 = new TestDictSheet.Elem {
                Value = 8
            };

            var elem3 = new TestDictSheet.Elem {
                Value = 65
            };

            row1.Arr.Add(elem1);

            row3.Arr.Add(elem2);
            row3.Arr.Add(elem3);

            _container.Dict.Add(row1);
            _container.Dict.Add(row2);
            _container.Dict.Add(row3);

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Dict.csv"), "Id,Dict,,,NestedDict,,,Value\n,A,B,C,2034\n,,,,1,2,3\nDict1,10,20,,X,YYY,ZZZZZ,0\nDict2,,20,10,,,,\nEmpty,,,,,,,8\n,,,,,,,65\n");
        }

        [Fact]
        public async Task TestExportVerticalCsv()
        {
            _container.Vertical = new TestVerticalSheet();

            var row1 = new TestVerticalSheet.Row()
            {
                Id = "Vertical1",
                Coord = new()
                {
                    new() { X = 1, Y = 2 },
                    new() { X = 2, Y = 3 },
                },
                Levels = new()
                {
                    new() { 1, 2, 3 },
                    new() { 4, 5 }
                }
            };

            var row2 = new TestVerticalSheet.Row()
            {
                Id = "Vertical2",
                Coord = new()
                {
                    new() { X = 1, Y = 2 },
                },
                Levels = new()
                {
                    null,
                    new() { 4, 5 }
                }
            };

            row2.Arr.Add(new() { Value = "Elem1" });
            row2.Arr.Add(new() { Value = "Elem2" });
            row2.Arr.Add(new() { Value = "Elem3" });

            _container.Vertical.Add(row1);
            _container.Vertical.Add(row2);

            _container.PostLoad();

            var result = await _container.Store(_converter);

            _logger.VerifyNoError();

            _fileSystem.VerifyTestData(Path.Combine("testdata", "Vertical.csv"), "Id,Coord:X,Coord:Y,Levels:1,Levels:2,Value\nVertical1,1,2,1,4,\n,2,3,2,5\n,,,3\nVertical2,1,2,,4,Elem1\n,,,,5,Elem2\n,,,,,Elem3\n");
        }
    }
}
