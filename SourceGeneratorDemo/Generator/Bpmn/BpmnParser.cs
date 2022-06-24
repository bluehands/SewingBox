using System.Collections.Immutable;
using System.Xml.Linq;

namespace Generator.Bpmn;

public static class BpmnParser
{
    public static ProcessDefinition GetProcessDefinition(string xml)
    {
        var doc = XDocument.Parse(xml);

        var startActivity = doc
            .Descendants()
            .Where(d => d.Name.LocalName == "startEvent")
            .Select(e =>
            {
                var fields = e.GetDescendants("formField")
                    .Select(f =>
                    {
                        var fieldId = f.GetAttributeValue("id");
                        return new FormFieldDefinition(
                            MapFieldType(f.GetAttributeValue("type"), f),
                            fieldId,
                            f.TryGetAttributeValue("label") ?? fieldId,
                            new(f.GetDescendants("constraint").Any(d => d.GetAttributeValue("name") == "required" && d.GetAttributeValue("config") == "true"),
                                                    f.GetDescendants("constraint").Any(d => d.GetAttributeValue("name") == "readonly" && d.GetAttributeValue("config") == "true")),
                            new(f.GetDescendants("property").Where(d => d.GetAttributeValue("id") == "type").Select(s => s.GetAttributeValue("value")).FirstOrDefault() ?? "")
                        );
                    }).ToImmutableArray();
                var formDefinition = new FormDefinition(fields);
                var activityId = e.GetAttributeValue("id");
                var activity = new StartActivityDefinition(activityId, e.TryGetAttributeValue("name") ?? activityId,
                    formDefinition);
                return activity;
            }).FirstOrDefault();

        if (startActivity == null)
        {
            throw new("No Start FehlerEvent was defined in workflow definition.");
        }

        var activityDefinitions = doc
            .Descendants()
            .Where(d => d.Name.LocalName == "userTask")
            .Select(e =>
            {
                var fields = e.GetDescendants("formField")
                    .Select(f =>
                    {
                        var fieldId = f.GetAttributeValue("id");
                        return new FormFieldDefinition(
                            MapFieldType(f.GetAttributeValue("type"), f),
                            fieldId,
                            f.TryGetAttributeValue("label") ?? fieldId,
                            new(f.GetDescendants("constraint").Any(d => d.GetAttributeValue("name") == "required" && d.TryGetAttributeValue("config") == "true"),
                                f.GetDescendants("constraint").Any(d => d.GetAttributeValue("name") == "readonly" && d.GetAttributeValue("config") == "true")),
                            new(f.GetDescendants("property").Where(d => d.GetAttributeValue("id") == "type").Select(s => s.GetAttributeValue("value")).FirstOrDefault() ?? "")
                        );
                    }).ToImmutableArray();
                var formDefinition = new FormDefinition(fields);
                var activityId = e.GetAttributeValue("id");
                var candidateGroups = e.TryGetAttributeValue("candidateGroups")?.SplitOmitEmptyTrim().ToImmutableArray() ?? ImmutableArray<string>.Empty;
                var documentation = e.GetDescendants("documentation").Select(d => d.Value).FirstOrDefault() ?? "";
                var activity = new ActivityDefinition(activityId, e.TryGetAttributeValue("name") ?? activityId, candidateGroups, formDefinition, documentation);
                return activity;
            } ).ToImmutableDictionary(k => new ActivityDefinitionId(k.Id), k => k);

        var enumList = doc
            .Descendants()
            .Where(d => d.Name.LocalName == "formField")
            .Where(w => w.GetAttributeValue("type") == "enum")
            .Select(s => (new PropertyId(s.GetAttributeValue("id")), GetEnumValues(s).ToImmutableDictionary(d => new EnumId(d.id), d => d.name)))
            .ToList();

        var enumDictionary = ImmutableDictionary<PropertyId, ImmutableDictionary<EnumId, string>>.Empty;
        enumList.ForEach(e => enumDictionary = enumDictionary.SetItem(e.Item1, e.Item2));

        return new (startActivity, activityDefinitions, enumDictionary);
    }

    static FieldType MapFieldType(string xmlFieldType, XElement formFieldElement)
    {
        return xmlFieldType switch
        {
            "string" => FieldType.String,
            "boolean" => FieldType.Bool,
            "enum" => FieldType.Enum(GetEnumValues(formFieldElement)),
            "date" => FieldType.Date,
            "long" => FieldType.Number,
            _ => throw new ArgumentOutOfRangeException(nameof(xmlFieldType), xmlFieldType, null)
        };
    }

    static IReadOnlyCollection<(string id, string name)> GetEnumValues(XElement formFieldElement) =>
        formFieldElement.GetDescendants("value")
            .Select(e =>
            {
                var id = e.GetAttributeValue("id");
                return (id, e.TryGetAttributeValue("name") ?? id);
            }).ToImmutableArray();
}

public static class XDocumentExtension
{
    public static string GetAttributeValue(this XElement element, string localName) =>
        element.Attributes().First(a => a.Name.LocalName == localName).Value;

    public static string? TryGetAttributeValue(this XElement element, string localName) =>
        element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value;

    public static IEnumerable<XElement> GetDescendants(this XElement element, string localName) =>
        element.Descendants().Where(d => d.Name.LocalName == localName);
}