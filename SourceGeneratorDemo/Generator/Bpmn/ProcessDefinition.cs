using System.Collections.Immutable;

namespace Generator.Bpmn;

public record ActivityDefinitionId(string Value)
{
    public override string ToString() => Value;
}

public record ActivityInstanceId(string Value)
{
    public override string ToString() => Value;
}

public record PropertyId(string Value)
{
    public override string ToString() => Value;
}

public record EnumId(string Value)
{
    public override string ToString() => Value;
}

public record ProcessDefinition(
    StartActivityDefinition StartActivity,
    ImmutableDictionary<ActivityDefinitionId, ActivityDefinition> ActivityDictionary, 
    ImmutableDictionary<PropertyId, ImmutableDictionary<EnumId, string>> EnumDictionary);

public record StartActivityDefinition(string Id, string Name, FormDefinition Form);
public record ActivityDefinition(string Id, string Name, IReadOnlyCollection<string> CandidateGroups, FormDefinition Form, string Description);

public record FormDefinition(IReadOnlyCollection<FormFieldDefinition> InputFields);

public record FormFieldValidation(bool IsRequired, bool IsReadonly);
public record FormFieldProperties(string Type);
public record FormFieldDefinition(FieldType FieldType, string FieldId, string Label, FormFieldValidation Validation, FormFieldProperties Properties);


[FunicularSwitch.Generators.UnionType]
public abstract class FieldType
{
    public static readonly FieldType String = new String_();
    public static readonly FieldType Bool = new Bool_();

    public static FieldType Enum(IReadOnlyCollection<(string id, string name)> values) => new Enum_(values);

    public static readonly FieldType Date = new Date_();
    public static readonly FieldType Number = new Number_();

    public class String_ : FieldType
    {
        public String_() : base(UnionCases.String)
        {
        }
    }

    public class Bool_ : FieldType
    {
        public Bool_() : base(UnionCases.Bool)
        {
        }
    }

    public class Enum_ : FieldType
    {
        public IReadOnlyCollection<(string id, string name)> Values { get; }

        public Enum_(IReadOnlyCollection<(string id, string name)> values) : base(UnionCases.Enum) => Values = values;
    }

    public class Date_ : FieldType
    {
        public Date_() : base(UnionCases.Date)
        {
        }
    }

    public class Number_ : FieldType
    {
        public Number_() : base(UnionCases.Number)
        {
        }
    }

    internal enum UnionCases
    {
        String,
        Bool,
        Enum,
        Date,
        Number
    }

    internal UnionCases UnionCase { get; }
    FieldType(UnionCases unionCase) => UnionCase = unionCase;

    public override string ToString() => System.Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
    bool Equals(FieldType other) => UnionCase == other.UnionCase;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FieldType)obj);
    }

    public override int GetHashCode() => (int)UnionCase;
}

public static class ProcessDefinitionExtension
{
	public static IEnumerable<FormFieldDefinition> GetFormFields(this ProcessDefinition processDefinition) =>
		processDefinition.ActivityDictionary
			.Values
			.SelectMany(v => v.Form.InputFields)
			.GroupBy(f => f.FieldId)
			.Select(g => g.First())
			.OrderBy(f => f.FieldId);
}