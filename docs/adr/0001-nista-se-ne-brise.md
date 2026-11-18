# Nothing is deleted — everything is deactivated

The brief requires an administrator to *deactivate* oglasi but leaves "managing user accounts" undefined. We chose to make **every** removal soft: one `Aktivan` flag on `Korisnik` and one on `Oglas`, used by administrators and by predavači withdrawing their own oglasi alike.

Hard deletes were the alternative. They were rejected because deleting an učenik would cascade away their ocene and silently move the prosečna ocena shown on unrelated predavači's public profiles, and because cascade behaviour across four relationships is a source of runtime failures we can't afford this close to the deadline. A boolean flag cannot fail that way, and it gives us one pattern instead of two.

**Consequence:** every query that reaches korisnici or oglasi must filter on `Aktivan`. That filter belongs in the repositories, not in the controllers, so it can't be forgotten. Statistics count active records only.

**Scope:** "every removal" means every removal of a **korisnik** or an **oglas** — the two things other people can see, and the two things ocene hang off. It does not reach an omiljeni row, which is one učenik's private bookmark: unmarking a predavač deletes the pair outright, because a bookmark nobody can see has nothing to preserve and no average to move. That is the only real delete in the application, and it lives in `OmiljeniRepository.UkloniAsync`.
