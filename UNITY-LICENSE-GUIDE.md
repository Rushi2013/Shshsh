# Teen GitHub secrets kahan se laayein? (Step by step)

Ye guide un teen secrets ke baare mein hai jo `.github/workflows/build-apk.yml` ko Unity cloud me APK banane ke liye chahiye:

| Secret | Kya hai | Kahan se milega |
| --- | --- | --- |
| `UNITY_LICENSE` | `.ulf` licence file ka **poora text** | Unity Hub (computer par) se free Personal licence activate karke |
| `UNITY_EMAIL` | Unity account ka email | https://id.unity.com |
| `UNITY_PASSWORD` | Unity account ka password | https://id.unity.com ( agar Google/Apple se signup kiya tha to yahin "set password" karein ) |

> **Sach ki baat:** Unity ab **Unity Personal ke liye manual activation (license.unity3d.com) support nahi karta**. Isliye `UNITY_LICENSE` banane ka official tarika sirf **Unity Hub** hai. Lekin niche ek **mobile-only community route** bhi hai jo GitHub Actions me try ki ja sakti hai.

---

## ⭐ Best route — no PC, no licence file (GameCI v6)

Unity ab Personal licence ke liye online account activation use karta hai; `.alf`/`.ulf` manual activation band ho chuka hai. GameCI ke naye version (`unity-builder@v6` + CLI) me **sirf email aur password** se Unity Personal activate hoti hai — koi licence file nahi chahiye [3](https://qiita.com/broken55/items/a7139f8be5bd9c003f31) [6](https://github.com/Andahmed/LegendsReborn/pull/48).

### Aapko bas ye karna hai

1. GitHub → ye repo → **Settings → Secrets and variables → Actions → New repository secret**.
2. Do secrets (pehle se daale hon to theek hai): `UNITY_EMAIL` aur `UNITY_PASSWORD`.
   - ⚠️ GameCI CLI ko **special characters wale passwords** me problem ho sakti hai. Aapne password chat me bhi likha hai — abhi ek **naya, sirf letters+numbers wala password** bana kar Unity par set karein aur GitHub me `UNITY_PASSWORD` update karein. Isse security bhi theek aur build bhi.
   - 2FA on ho to temporary off karein ya build fail hone par log bhejein.
3. **Actions → Build Android APK → Run workflow** → branch `arena/411eaf4f-shshsh` → Run.
4. Build khatam hone par **Releases** ya **Artifacts** se `CityDrive.apk` download karein.

Agar isme error aaye to run ka last error line mujhe bhej dein — main workflow tune kar dunga. Official fallback (Unity Hub se `.ulf`) neeche diya hai, lekin uske liye PC chahiye.

---

## Step 1 — Unity account ready karein (mobile se)

1. Browser me https://id.unity.com kholein aur apne account se login karein.
2. Agar aapne Google/Apple/Facebook se signup kiya tha aur password set nahi kiya, to **Account Settings → Security → Set / Reset password** se ek password bana lein. Yehi password `UNITY_PASSWORD` hoga.
3. Email address exactly wahi use karein jise aap `UNITY_EMAIL` me daalenge.
4. Agar account par **2FA (two-factor authentication)** on hai to CI build login ke step par fail ho sakta hai (robot aapke phone ka code nahi daal sakta). Build ke dauran error aaye to pehle 2FA temporarilyOff karke try karein.

## Step 2 — Kisi computer par Unity Hub se .ulf licence file banayein

1. Computer me https://unity.com/download se **Unity Hub** install karein (sirf Hub; Unity Editor install mat karein).
2. Hub kholein → apne **usi Unity account** se sign in karein jiska email/password aapne Step 1 me ready kiya.
3. Hub me **Settings (gear icon) → Licenses → Add license** (ya Preferences → Licenses → Add) par jayein.
4. **Get a free personal license** chunhein → **Agree and get personal edition license** dabayein.
   - Agar licence pehle se list me dikhe to bhi **Add** zaroor dabayein — tabhi `.ulf` file banegi.
5. Ab `.ulf` file yahan milti hai:
   - **Windows:** `C:\ProgramData\Unity\Unity_lic.ulf`
   - **macOS:** `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Linux:** `~/.local/share/unity3d/Unity/Unity_lic.ulf`
   - (Hidden folders dikhane honge; Windows me File Explorer → View → Show → Hidden items.)
6. File ko **Notepad / Text Editor** me open karein aur **poora content copy** karein (XML se shuru se end tak). Yehi `UNITY_LICENSE` ka value hai.
   -agar `UnityEntitlementLicense.xml` mile aur `.ulf` na mile, to Step 3–4 dobara karein; CI ko `.ulf` chahiye.
7. Important: `.ulf` copy karne ke baad Unity Hub se **sign out** kar dein, warna local seat CI build ko block kar sakta hai.

## Step 3 — GitHub me 3 secrets add karein (mobile/browser se)

1. GitHub app/browser → ye repo (`Rushi2013/Shshsh`) kholein.
2. **Settings → Secrets and variables → Actions** par jayein.
3. **New repository secret** dabayein aur ek-ek karke ye teen banayein (naam bilkul same, capital letters ke saath):

   1. Name: `UNITY_LICENSE` → Secret: Step 2 me copy kiya hua **poora `.ulf` text** paste karein.
   2. Name: `UNITY_EMAIL` → Secret: aapka Unity login email.
   3. Name: `UNITY_PASSWORD` → Secret: aapka Unity password.

4. **Add secret** dabayein. Screen par value chhupi dikhegi — yahi sahi hai.

> Secrets ko kabhi bhi chat, workflow file ya code me na likhein. Sirf GitHub ke secret box me daalein.

## Step 4 — APK build karein

1. Repo me **Actions** tab → left sidebar me **Build Android APK** → **Run workflow** → branch select karein (`arena/411eaf4f-shshsh`) → green **Run workflow** button.
2. Build 10–30 minute le sakta hai (Unity install + city import + IL2CPP). Run par click karke live logs dekhein.
3. Success hone par:
   - **Releases** me `City Drive - Android APK` open karein → `CityDrive.apk` download, ya
   - workflow run ke bottom me **Artifacts → CityDrive-APK**.
4. Phone me APK open karein → “Install unknown apps” allow karein → Install → game khelin.

## Agar build fail ho

- **License / activation error:** `UNITY_LICENSE` me poora XML hai ya nahi check karein; Hub se sign out karke naya `.ulf` banayein; email/password sahi aur 2FA off hai ya nahi dekhein.
- **"Unity version not found":** workflow me `unityVersion: 2022.3.62f3` hai — ye version game-ci support karta hai. Log me exact error mujhe bhej dein to main fix kar dunga.
- **Compile error:** error line number ke saath log mujhe bhej dein; main code fix karke push kar dunga.

## Alternatives (PC ke bina, par paid/community)

- **Unity Cloud Build**: dashboard.unity3d.com se repo connect karke build kar sakte hain, par cloud build automation paid plans (Pro/Enterprise) ka feature hai — free Personal me reliable option nahi hai.
- **Community tool `unity-license-activate`** (GameCI): email/password se `.ulf` automatically generate karta hai, lekin 2FA/email verification ki wajah se flaky ho sakta hai aur official Unity flow nahi hai. Agar aap ye try karna chahte hain to batayein — main aapke repo me workflow bana ke de dunga (aapko sirf 2 secrets daalne honge).

Official references: [GameCI activation guide](https://game.ci/docs/github/activation/) · [Unity Hub licence management](https://docs.unity.com/en-us/hub/manage-license) · [Unity manual activation limits](https://docs.unity.com/en-us/engine/6000.0/manual/get-started/install-and-upgrade/licenses-and-activation/manual-license-activation/manual-activation-hub)
