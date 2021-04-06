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
        public async Task IfNullTest1() {
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task IfNullTest2() {
            var test = @"
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:a == null|}) {
                return false;
            } else {
                 a.Substring(0);
                 return true;
            }
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
            a.Substring(0);
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task IfNotNullTest3() {
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
            } else {
                return true;
            }
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
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ConditionalOperatorCheck5() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            return ({|#0:a.Equals(null)|} ? true : false);
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCoalesceRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCoalesceRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ReferenceEqualsNullCheck1() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:ReferenceEquals(a, null)|}) {
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

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task ReferenceEqualsNullCheck2() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        bool Test(string a) {
            if ({|#0:ReferenceEquals(null, a)|} || a.Length > 3) {
                return false;
            }
            return true;
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullCheckRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task NullPropagation1() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int? al = {|#0:a?.Length|};
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
            int? al = a.Length;
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullPropagationRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task NullPropagation2() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            string? al = {|#0:a?.Substring(0)|};
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
            string? al = a.Substring(0);
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullPropagationRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task NullPropagation3() {
            var test = @"
using System;
#nullable enable

namespace ConsoleApplication1
{
    class TypeName
    {   
        void Test(string a) {
            int? al = {|#0:a?.Substring(0).Length|};
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
            int? al = a.Substring(0).Length;
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NullCheckCsAnalyzerAnalyzer.NullPropagationRule).WithLocation(0).WithArguments("a");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
