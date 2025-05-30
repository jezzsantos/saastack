using FluentAssertions;
using Infrastructure.Persistence.Common.Extensions;
using QueryAny;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class DynamicLinqWhereExtensionsSpec
{
    [Fact]
    public void WhenToDynamicLinqWhereClauseAndSingleCondition_ThenReturnsLinq()
    {
        var wheres = new List<WhereExpression>
        {
            new()
            {
                Condition = new WhereCondition
                {
                    FieldName = "afield1",
                    Operator = ConditionOperator.EqualTo,
                    Value = "astringvalue"
                }
            }
        };

        var result = wheres.ToDynamicLinqWhereClause();

        result.Should().Be("Value.ContainsKey(\"afield1\") && String(Value[\"afield1\"]) == \"astringvalue\"");
    }

    [Fact]
    public void WhenToDynamicLinqWhereClauseAndMultipleConditions_ThenReturnsLinq()
    {
        var wheres = new List<WhereExpression>
        {
            new()
            {
                Condition = new WhereCondition
                {
                    FieldName = "afield1",
                    Operator = ConditionOperator.EqualTo,
                    Value = "astringvalue"
                }
            },
            new()
            {
                Operator = LogicalOperator.And,
                Condition = new WhereCondition
                {
                    FieldName = "afield2",
                    Operator = ConditionOperator.GreaterThanEqualTo,
                    Value = "astringvalue"
                }
            }
        };

        var result = wheres.ToDynamicLinqWhereClause();

        result.Should()
            .Be(
                "Value.ContainsKey(\"afield1\") && String(Value[\"afield1\"]) == \"astringvalue\" and Value.ContainsKey(\"afield2\") && String(Value[\"afield2\"]) >= \"astringvalue\"");
    }

    [Fact]
    public void WhenToDynamicLinqWhereClauseAndNestedConditions_ThenReturnsLinq()
    {
        var wheres = new List<WhereExpression>
        {
            new()
            {
                Condition = new WhereCondition
                {
                    FieldName = "afield1",
                    Operator = ConditionOperator.EqualTo,
                    Value = "astringvalue"
                }
            },
            new()
            {
                Operator = LogicalOperator.And,
                NestedWheres =
                [
                    new WhereExpression
                    {
                        Condition = new WhereCondition
                        {
                            FieldName = "afield2",
                            Operator = ConditionOperator.EqualTo,
                            Value = "astringvalue2"
                        }
                    },

                    new WhereExpression
                    {
                        Operator = LogicalOperator.Or,
                        Condition = new WhereCondition
                        {
                            FieldName = "afield3",
                            Operator = ConditionOperator.EqualTo,
                            Value = "astringvalue3"
                        }
                    }
                ]
            }
        };

        var result = wheres.ToDynamicLinqWhereClause();

        result.Should()
            .Be(
                "Value.ContainsKey(\"afield1\") && String(Value[\"afield1\"]) == \"astringvalue\" and (Value.ContainsKey(\"afield2\") && String(Value[\"afield2\"]) == \"astringvalue2\" or Value.ContainsKey(\"afield3\") && String(Value[\"afield3\"]) == \"astringvalue3\")");
    }
}