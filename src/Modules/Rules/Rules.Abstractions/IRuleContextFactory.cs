namespace Ecm.Rules.Abstractions;

public interface IRuleContextFactory
{
    IRuleContext FromDictionary(IDictionary<string, object> data);
    IRuleContext FromObject(object source);
}
