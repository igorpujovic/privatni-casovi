using Microsoft.AspNetCore.Mvc;

namespace Privatni_casovi.ModelBinding;

/// <summary>
/// The messages model binding produces when a posted value cannot become the property it was
/// posted to, said in Serbian.
/// </summary>
/// <remarks>
/// <para>
/// These are the errors that happen <i>before</i> any annotation runs: a <c>Lokacija</c> that
/// is not one of the three, a <c>Uloga</c> that is not one of the two,
/// <c>PredmetId=abc</c> where an int was expected. <c>[Required]</c> and <c>[Range]</c> never
/// see those values, so nothing on the ViewModel gets a chance to word them — the framework
/// answers first, and its answer is English: „The value '77' is invalid.“ on a form whose
/// every other message is Serbian.
/// </para>
/// <para>
/// Every one of them is reachable only by a crafted POST, because a <c>&lt;select&gt;</c>
/// offers nothing else. That is exactly why they are worth wording: a crafted
/// <c>Uloga=Administrator</c> is the request <see cref="ViewModels.UlogaIzbor"/> exists to
/// refuse, and refusing it in English is the one screen where the refusal is visible.
/// </para>
/// <para>
/// Set once on <see cref="MvcOptions"/> rather than per property, which is what makes this a
/// sweep rather than a patch: it covers the fields the application has today and every field a
/// later form adds, and there is no annotation for a call site to forget. The one binder
/// message the application words for itself stays where it is —
/// <see cref="DecimalModelBinder"/> writes its own „Unesite broj, sa tačkom…“ because a cena
/// has a decimal separator to explain and a generic „mora biti broj“ does not explain it.
/// </para>
/// </remarks>
public static class PorukeVezivanja
{
    /// <summary>Said of a value that is not the field's to hold, wherever it arrived.</summary>
    /// <remarks>
    /// Two of the accessors below want exactly this sentence — one for a property of a
    /// ViewModel, one for a bare action parameter — and they say it once.
    /// </remarks>
    private static string NijeIspravna(string? vrednost)
        => $"Vrednost „{vrednost}“ nije ispravna.";

    public static void NaSrpskom(MvcOptions opcije)
    {
        ArgumentNullException.ThrowIfNull(opcije);

        var poruke = opcije.ModelBindingMessageProvider;

        // The value the browser sent is echoed back, along with the [Display(Name = ...)] of
        // the field, so the korisnik can see what is being complained about. Echoing it is
        // safe: the tag helper that renders a validation message HTML-encodes it, so a
        // Lokacija of "<script>…" comes back as text on the form and not as markup.
        poruke.SetAttemptedValueIsInvalidAccessor(
            (vrednost, polje) => $"Vrednost „{vrednost}“ nije ispravna za polje {polje}.");

        poruke.SetValueIsInvalidAccessor(NijeIspravna);

        poruke.SetUnknownValueIsInvalidAccessor(
            polje => $"Vrednost poslata za polje {polje} nije ispravna.");

        poruke.SetValueMustBeANumberAccessor(
            polje => $"Polje {polje} mora biti broj.");

        poruke.SetValueMustNotBeNullAccessor(
            _ => "Ovo polje je obavezno.");

        poruke.SetMissingBindRequiredValueAccessor(
            polje => $"Zahtev ne sadrži vrednost za polje {polje}.");

        poruke.SetMissingKeyOrValueAccessor(
            () => "Ovo polje je obavezno.");

        poruke.SetMissingRequestBodyRequiredValueAccessor(
            () => "Zahtev je stigao bez sadržaja.");

        // The „non-property“ variants are the same errors on an action parameter that is not a
        // property of a ViewModel — `int id` on the two Aktiviraj actions, say. Nothing renders
        // them today, and wording them is what keeps that true if something ever does.
        poruke.SetNonPropertyAttemptedValueIsInvalidAccessor(NijeIspravna);

        poruke.SetNonPropertyValueMustBeANumberAccessor(
            () => "Vrednost mora biti broj.");

        poruke.SetNonPropertyUnknownValueIsInvalidAccessor(
            () => "Poslata vrednost nije ispravna.");
    }
}
