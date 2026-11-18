using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Privatni_casovi.ModelBinding;

/// <summary>
/// Reads every <c>decimal</c> out of a request with <see cref="CultureInfo.InvariantCulture"/>,
/// whatever culture the machine running the application happens to be set to.
/// </summary>
/// <remarks>
/// <para>
/// This closes a gap between two halves of the framework that disagree. An
/// <c>&lt;input type="number"&gt;</c> is <i>defined</i> to carry a dot-decimal value — the
/// browser may show a comma to a Serbian user, but what it submits is <c>2500.50</c> — and
/// the tag helper renders the value back the same way. Model binding, meanwhile, parses form
/// values with <c>CultureInfo.CurrentCulture</c>. On a machine set to Serbian that culture
/// reads the dot as a <i>thousands</i> separator, so <c>2500.50</c> binds as
/// <c>250050</c> and a predavač publishes a čas at a hundred times the cena they typed. No
/// validation catches it, because 250050 is a perfectly good decimal.
/// </para>
/// <para>
/// Binding invariantly is the half to change: it matches what the browser sends, it matches
/// what the tag helper writes back into the field on a re-render, and it makes the cena of an
/// oglas mean the same thing on the developer's machine and on the machine this is handed in
/// on. Display stays cultured — the tables format cena with <c>N2</c> and read
/// <c>3.100,00 RSD</c> as they should.
/// </para>
/// <para>
/// Registered for <c>decimal</c> and <c>decimal?</c> in <c>Program.cs</c>, so it covers the
/// cena on this form and the maksimalna cena filter on the pretraga alike.
/// </para>
/// </remarks>
public class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var vrednost = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (vrednost == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, vrednost);

        var unos = vrednost.FirstValue;

        // A field left blank is not a zero. Leaving the result unset keeps the property at its
        // default so [Required] is the one that speaks, in the words the form already uses.
        if (string.IsNullOrWhiteSpace(unos))
        {
            return Task.CompletedTask;
        }

        // Float rather than Number: it is exactly what an <input type="number"> can produce —
        // sign, digits, one dot — and no thousands separator. Allowing separators would let a
        // hand-crafted "2500,50" through as 250050 rather than as the error it is.
        if (decimal.TryParse(unos, NumberStyles.Float, CultureInfo.InvariantCulture, out var broj))
        {
            bindingContext.Result = ModelBindingResult.Success(broj);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Unesite broj, sa tačkom kao decimalnim znakom.");
        }

        return Task.CompletedTask;
    }
}

/// <summary>Hands <see cref="DecimalModelBinder"/> every <c>decimal</c> and <c>decimal?</c>.</summary>
public class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType == typeof(decimal)
            || context.Metadata.ModelType == typeof(decimal?)
                ? new DecimalModelBinder()
                : null;
    }
}
