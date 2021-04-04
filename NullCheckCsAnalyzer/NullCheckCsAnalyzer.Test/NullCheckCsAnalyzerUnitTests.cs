using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = NullCheckCsAnalyzer.Test.CSharpCodeFixVerifier<
    NullCheckCsAnalyzer.NullCheckCsAnalyzerAnalyzer,
    NullCheckCsAnalyzer.NullCheckCsAnalyzerCodeFixProvider>;

namespace NullCheckCsAnalyzer.Test {
    [TestClass]
    public class NullCheckCsAnalyzerUnitTest {
        [TestMethod]
        public async Task NullableDisabledTest() {
            var test = @"
namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if (a == null) {
                return false;
            }
            return true;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NullableTypeTest() {
            var test = @"
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string? a) {
            if (a == null) {
                return false;
            }
            return true;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task IfNullTest() {
            var test = @"
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a == null|}) {
                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task IfNotNullTest() {
            var test = @"
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a != null|}) {
                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            return false;
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
