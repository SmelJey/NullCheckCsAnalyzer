using System.IO;
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
        public async Task IfNotNullTest1() {
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

        [TestMethod]
        public async Task IfNotNullTest2() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a != null|}) {
                Console.WriteLine(1);
                Console.WriteLine(2);

                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            Console.WriteLine(1);
            Console.WriteLine(2);

            return false;
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ExpressionNullCheck1() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if (5 > 3 && {|#0:a != null|}) {
                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if (5 > 3 && true) {
                return false;
            }
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ExpressionNullCheck2() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a == null|} && 5 > 3) {
                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if (false && 5 > 3) {
                return false;
            }
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ConditionalOperatorCheck1() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            return ({|#0:a == null|} ? true : false);
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            return (false);
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ConditionalOperatorCheck2() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int c = {|#0:a != null|} ? 4 : 3;
            return;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int c = 4;
            return;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ConditionalOperatorCheck3() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int c = {|#0:a != null|} && a.Length > 2 ? 4 : 3;
            return;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int c = true && a.Length > 2 ? 4 : 3;
            return;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ConditionalOperatorCheck4() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int c = ({|#0:a != null|}) ? 4 : 3;
            return;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int c = 4;
            return;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task CoalesceOperator() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            string c = {|#0:a ?? string.Empty|};
            return;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            string c = a;
            return;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task CoalesceAssignment() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            {|#0:a ??= string.Empty|};
            return;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            return;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task EqualsNullCheck1() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a.Equals(null)|}) {
                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
using System;
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
        public async Task EqualsNullCheck2() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a.Equals(null)|} || a.Length > 3) {
                return false;
            }
            return true;
        }
    }
}";

            var fixtest = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if (false || a.Length > 3) {
                return false;
            }
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("NullCheckCsAnalyzer").WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
