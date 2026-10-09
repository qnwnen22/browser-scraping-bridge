#nullable disable warnings
using System.Linq.Expressions;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class LinqExtension
    {
        public static List<string> GetMemberNames<T>(this T model, Expression<Func<T, object>> selector) where T : class
        {
            var columns = new List<string>();

            if (selector.Body is MemberExpression member)
            {
                // 단일 프로퍼티: x => x.Idx
                columns.Add(member.Member.Name);
            }
            else if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            {
                // 단일 프로퍼티(박싱된 경우): x => (object)x.Idx
                columns.Add(unaryMember.Member.Name);
            }
            else if (selector.Body is NewExpression newExpr)
            {
                // 익명 객체: x => new { x.Idx, x.UserId }
                foreach (var arg in newExpr.Arguments)
                {
                    if (arg is MemberExpression mem)
                        columns.Add(mem.Member.Name);
                }
            }
            else if (selector.Body is MemberInitExpression initExpr)
            {
                // MemberInit: x => new SomeDto { A = x.Idx, B = x.UserId }
                foreach (var binding in initExpr.Bindings)
                    columns.Add(binding.Member.Name);
            }
            else if (selector.Body is NewArrayExpression arrExpr)
            {
                // 배열 방식: x => new object[] { x.Idx, x.UserId }
                foreach (var exp in arrExpr.Expressions)
                {
                    if (exp is MemberExpression mem)
                        columns.Add(mem.Member.Name);
                }
            }

            return columns;
        }
    }
}
