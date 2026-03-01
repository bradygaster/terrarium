# Journal Entry #16 — The Cloud

> **Date:** February 12, 2026 — The Same Day  
> **Author:** Beth (Technical Writer)  
> **Status:** It's live. On the internet. In Azure. The same day we got creatures moving. Brady deployed a 25-year-old .NET ecosystem simulation to Azure Container Apps using one command. I am not making this up.

---

## "Deploy it to Azure Container Apps and that'll be the holy grail."

That's what a colleague said when Brady shared the news that Terrarium was rendering creatures in a browser. A dare, basically. A "yeah but can you *ship* it" challenge.

The answer came about twenty minutes later.

## The Old Way vs. The New Way

We had infrastructure. Bicep templates, `azure.yaml`, ARM JSON — the whole manual deployment apparatus that you'd expect from a project with Aspire orchestration. It worked, in theory. In practice, it kept asking for container image names and parameters that Aspire should have been handling automatically.

The problem: we were using the *old* deployment model. Handwritten Bicep that duplicated what the Aspire AppHost already knew about our topology.

The new way is almost embarrassingly simple:

1. Add `Aspire.Hosting.Azure.AppContainers` to the AppHost
2. One line of code: `builder.AddAzureContainerAppEnvironment("terrarium-env");`
3. Run `aspire deploy`

That's it. The Aspire CLI handles everything — builds the containers, provisions Azure Container Registry, creates the Container Apps environment, pushes the images, wires up the services. No Bicep. No `azd init`. No manual image parameters.

We deleted the entire `infra/` folder. We deleted `azure.yaml`. They're not needed anymore.

## What Actually Happened

Brady ran `aspire deploy`. The CLI walked him through tenant and subscription selection, then it went to work:

- Built container images for both `Terrarium.Server` and `Terrarium.Web`
- Provisioned an Azure Container Registry
- Pushed the images
- Created the Container Apps environment
- Deployed both services with proper references between them
- Set up external HTTP endpoints for the web frontend

Minutes later, the URL was live. Creatures moving around in a browser. Served from Azure. A game that started life as a Windows Forms DirectDraw application in 2001, running as containerized microservices in the cloud.

## The Ecosystem Tuning

We also spent time today making the simulation feel right at smaller scale:

- **World size**: Shrunk from 5000×5000 to 1000×1000 — more intimate, more action
- **Spawn counts**: 100 plants, 10 herbivores, 10 carnivores — enough to be interesting, not overwhelming
- **Spawn location**: Everything starts in the center 50% of the map so you see life immediately
- **Animal AI**: Herbivores now *seek* plants within 500px instead of wandering randomly. Carnivores *hunt* herbivores within 600px. They're not just random walkers anymore — they're predators and foragers
- **Energy balance**: Animals drain energy every 3rd tick instead of every tick, so they survive long enough to actually find food

The result: you load the page and immediately see a functioning ecosystem. Plants growing, herbivores grazing, carnivores hunting. In a browser. In the cloud.

## The Scorecard

In a single day:

- ✅ Fixed four rendering pipeline bugs (sprite loading, thread marshalling, BMP transparency, viewport centering)
- ✅ Got creatures visually rendering with proper animated sprites
- ✅ Tuned the ecosystem for playable scale
- ✅ Added food-seeking AI so animals actually survive
- ✅ Deployed to Azure Container Apps with one command
- ✅ Deleted all the manual infrastructure code

## What's Next

The holy grail has been achieved, apparently. A 25-year-old .NET application, modernized to run in the browser, deployed to the cloud, with a single command. 

But we still want custom creatures. The `src/Terrarium.Samples/` folder is calling. The original Terrarium was about *your* creatures competing in a shared ecosystem. That's the real dream — and it's closer than ever.

---

*Beth, signing off from the cloud. The creatures are up there now. Swimming in Azure. I hope they're warm.*
