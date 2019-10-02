[![Ka-ching logo](https://ka-ching.dk/media/4o4gwh5o/ka-ching-logo-black-1x.svg)](https://ka-ching.dk)

[Ka-ching](https://ka-ching.dk) is a digital sales tool for retailers that bridges retail and online through assisted selling.

# Episerver Add-On

![Admin Plug-In Screenshot](https://raw.githubusercontent.com/ka-ching-as/ka-ching-episerver-app/master/Images/EpiserverKachingPlugIn.png)
 
## Concept

This Episerver add-on aims at making it easy to use the awesome capabilities and information in Episerver Commerce as a sales tool in retail stores by integrating with Ka-ching.

When the add-on is installed it listens for commerce data change events and if configured with import URLs it will send the updated data to Ka-ching. See below for details of what data and what events are handled. To configure the add-on go to the CMS admin page and find "Ka-ching Integration" in the Tools section. See below for more details.

The current implementation is developed and tested using the Episerver reference commerce site [Quicksilver](https://github.com/episerver/Quicksilver). In it's current state it this add-on is mostly a reference point and accelerator for further implementation and customization in real case scenarios.

All development on this add-on is open source under the MIT license. Contributions are very welcome.

Our apps are not in the iOS App Store yet, so [contact us](https://ka-ching.dk/about-us/) to get hold of the install links.

## Configuring the add-on

This section assumes you have a Ka-ching account. Get in touch using the contact details found [here](https://ka-ching.dk/about-us/) if you need an account.

Go to [Ka-ching Backoffice](https://backoffice.ka-ching.dk/login) to setup the import integrations needed for this add-on to work.

After logging in, click the user icon in the top bar and click the "Advanced" option to show the "Advanced" menu in the left of the screen.

Click the "Import integrations" option and create the following integrations:

* Products import
* Tags import
* Folders import

Copy and paste each of the URLs for those endpoints to their appropriate input fields in the Episerver add-on. Once those URLs are saved it will be possible to do an initial full export of products and categories from Episerver to Ka-ching. 

Next step is to configure matching master data in Ka-ching.

## Manual setup in Ka-ching

This section assumes you have a Ka-ching account. Get in touch using the contact details found [here](https://ka-ching.dk/about-us/) if you need an account.

Go to [Ka-ching Backoffice](https://backoffice.ka-ching.dk/login) to manage the manual data maintenance listed below. 

More info on how to use the Ka-ching Backoffice can be found on our [Zendesk site](https://ka-ching.zendesk.com).

* Create default taxes that will be applied to all products.
* Create markets with matching ids. Select appropriate default taxes for the new markets.
* Create and configure shops. Select appropriate market for each of the shops.
* Create cashiers for each shop.
* Create and invite the appropriate users needed. Ususally a shop manager for each shop is enough.

That should do it.

## Handled data

### Products

 * Display name for all defined languages.
 * Prices for all markets.
 * First image present if any.
 * All parent categories are added as tags to enable folder structure in Ka-ching.

### Variations

 * Display name for all defined languages.
 * Prices for all markets.
 * First image present if any.

### Categories

 * Display name for all defined languages.

## Assumptions and limitations
This solution assumes the following:

- Products only exist 1 place in the category structure. That is that there are no duplicates of products with the same `Code`
- Products only have variations as children.
- Products only have categories as parents.
- Variations only have products as parents.
- Categories only have categories as parents.
- Only 1 catalog.

We might be able to remove some or all of those in future versions.

### Note on prices

Ka-ching prices are including VAT style taxes and excluding "sales tax" style taxes. This solution assumes that the value in IPriceAmount.UnitPrice.Amount uses the same semantics.

### Custom properties

This solution also doesn't handle any custom properties defined on products, variations or categories. There are some comments in the code to show how size, color and description, as defined in Quicksilver, could be handled.

### Specialized taxes per product

Ka-ching is capable of handling 

### Bundles and packages

Ka-ching doesn't support product bundles or packages out of the box, so these have been skipped in this version. It might be added in a future version.

## Handled events

- PriceUpdated
- MovedContent
- PulibshedContent
- DeletedContent
- DeletingContent

## Logging in host project

In the Quicksilver solution go to the EPiServerLog.config file in the EPiServer.Reference.Commerce.Site project and and these lines inside the `<log4net>` tag.

```
<logger name="KachingPlugIn">
   <level value="Info" />
   <appender-ref ref="debugLogAppender" />
</logger>
```

## Building and installing locally

### Building

- Clone or download this repo.
- Open KachingPlugIn.sln in Visual Studio 2017.
- Go to the bottom of the file Main.targets in the Packager folder and change the value of `DestinationFolder` in this tag `<Copy  SourceFiles="@(_CopyItems)" DestinationFolder="C:\Users\Bruger\nugets" />`to match the path of your local nuget folder.
- Ctrl-Shift-B to build solution. The package dependencies should be downloaded automatically based on the packages.config file.

### Running

- Open your favorite Episerver Commerce project.
- Make sure you have a NuGet feed that's looking in the folder specified above.
- In the Package Manager Console run the command `install-package KachingPlugIn`.
- Build solution.
- Refresh your browser or debug solution if IIS Express is not already running.

## Ideas for future versions

- Handling of custom properties on products and variations.
- Handling of tax data per product.
- Bundles and packages.
- Synchronization of stock values to Ka-ching.
- Synchronization of campaigns.
- Synchronization of markets and default taxes
