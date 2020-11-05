[![Ka-ching logo](https://ka-ching.dk/media/4o4gwh5o/ka-ching-logo-black-1x.svg)](https://ka-ching.dk)

[Ka-ching](https://ka-ching.dk) is a digital sales tool for retailers that bridges retail and online through assisted selling.

# Episerver Add-On v3.0.0

![Admin Plug-In Screenshot](https://raw.githubusercontent.com/ka-ching-as/ka-ching-episerver-app/master/Images/EpiserverKachingPlugIn.png)
 
## Concept

This Episerver add-on aims at making it easy to use the awesome capabilities and information in Episerver Commerce as a sales tool in retail stores by integrating with Ka-ching.

When the add-on is installed it listens for commerce data change events and if configured with import URLs it will send the updated data to Ka-ching. Version 3 of the Add-On also makes it easy to send sales data to Episerver from Ka-ching as well as enabling customer lookup in Ka-ching with minimal effort. See below for details of what data and what events are handled. 

Configuration of the add-on is done post-install in a Ka-ching related section in web.config. See below for more details. To perform initial exports to Ka-ching go the CMS admin page and find "Ka-ching Integration" in the Tools section. 

The current implementation is developed and tested using the Episerver reference commerce site [Foundation](https://github.com/episerver/Foundation).

All development on this add-on is open source under the MIT license. Contributions are very welcome.

You need a Ka-ching account to use this Add-On. If you don't have one then [contact us](https://ka-ching.dk/about-us/) to learn more.

If you have a Ka-ching account you can download the [Ka-ching POS app](https://apps.apple.com/us/app/ka-ching-pos-point-of-sale/id1474762454) and get started utilizing Episerver in your stores right away.

## Configuring the add-on

This section assumes you have a Ka-ching account. Get in touch using the contact details found [here](https://ka-ching.dk/about-us/) if you need an account.

### Episerver

The Ka-ching Add-On for Episerver consists of 2 NuGet packages. 1 for CMS and 1 for Commerce. You can find them on the [Episerver NuGet feed](https://nuget.episerver.com/?q=kaching&s=Popular&r=10&f=All). Currently the Ka-ching Add-On works with Episerver.CMS version 11 and Episerver.Commcerce version 13.

#### Data synchronization from Episerver to Ka-ching

TODO
After installing the NuGet packages in the relevant Episerver projects you should see a `kaching` section of the web.config in the CMS project:

```xml
<kaching productsImportUrl=""
       productAssetsImportUrl=""
       productRecommendationsImportUrl=""
       foldersImportUrl=""
       tagsImportUrl=""
       exportSingleVariantAsProduct="false"
       listenToRemoteEvents="true">
	<systemMappings barcodeMetaField=""
	                descriptionMetaField=""/>
	<attributeMappings>
	  <!--<add metaField="Brand" attributeId="brand"/>-->
	</attributeMappings>
</kaching>
```

In this section you need to specify URL endpoints for each data entity you wish to synchronize to Ka-ching. See more on how to obtain those below.

Ka-ching supports products without variants and if you have products with just 1 variant you can force those to be just products in Ka-ching by setting `exportSingleVariantAsProduct` to `true`. This makes it possible to tap the product in Ka-ching to put it in the basket as opposed to opening the product details page where you have to choose the variant.

To receive events from commerce data changes happing in the Commerce Manager set `listenToRemoteEvents` to `true` - if you're running more than 1 front end in load balanced setup take care that only one of these instances have this enabled since the Commerce Manager will send push notifications to all front ends.

For all products and variants you can use the `barcodeMetaField` and `descriptionMetaField` attributes of the `systemMappings` mappings element to tell the Ka-ching Add-On where to look for those pieces of information and place them correctly in data sent to Ka-ching. If for example you store the barcode information that should be used for scanning in the stores in a field called `EAN` on the products and variants, then you specify `barcodeMetaField="EAN"`. Those two fields should be normal strings, but we do however try to convert XhtmlString to a string by stripping tags.

If you have custom attributes on your products and variants you can map those to their Ka-ching using the `attributeMappings` element. The `metaField` is the name of the property on your product or variant and `attributeId` is the id of the matching attribute define in Ka-ching. You'll have to define those manually in Ka-ching Backoffice.

#### Endpoints in Episerver

The Ka-ching Add-On sets up two endpoints for communicting with Ka-ching:

* Customer search based on a search string from Ka-ching 
* Real time reception of data from checkouts done in the physical stores

To enable these endpoints you have to enable KachingApiKeyAuthentication which is done by activating in on the app builder in Startup.cs. You can use any string as API key. It is passed in the Authorization header in the HTTP calls from Ka-ching. See below for more on how to configure the Authorization header on the Ka-ching side.

```
app.UseKachingApiKeyAuthentication(
    new KachingApiAuthenticationOptions
    {
        ApiKey = "24E40822-228C-4D56-9BCD-9D99D2C10716"
    });
```

### Ka-ching

Go to [Ka-ching Backoffice](https://backoffice.ka-ching.dk/login) to setup the import integrations needed for this add-on to work.

After logging in, click the user icon in the top bar and click the "Advanced" option to show the "Advanced" menu in the left of the screen.

Click the "Import integrations" option. The Add-on supports synchronization to Ka-ching using the following integration points:

* Products import
* Product assets import
* Recommendation import
* Tags import
* Folders import

Copy and paste each of the URLs for those endpoints to their appropriate fields in the web.config configuration in Episerver. Once those URLs are saved it will be possible to do an initial full export of products, categories, document/image assets and product relations from Episerver to Ka-ching from the `Ka-ching integration` page in CMS admin.

To enable customer search in Episervers customer database from Ka-ching go to the "Runtime integrations" menu option. Click "Add integration" and choose "Add customer lookup integration". Give it a name, an id and enter the URL of the endpoint in Episerver. HTTP method should be GET. The URL will have the form `https://<episerver-host>/api/kaching/customerlookup`. Click "Add HTTP header" and specify `Authorization` as header name with `KachingKey <episerver-api-key>` where `<episerver-api-key>` is the API key mention above. Click "Add query parameter" and specify `q` as the name with `{search_term}` as the value.

To enable real time checkout data from Ka-ching go to the "Export integrations" menu option. The setup is similar very to the setup for the customer lookup integration. Click "Add web hook" and choose "Add sale integration". Give it a name, an id and enter the URL of the endpoint in Episerver. The URL wil have the form `https://<episerver-host>/api/kaching/sales`. Click "Add HTTP header" and specify `Authorization` as header name with `KachingKey <episerver-api-key>` where `<episerver-api-key>` is the API key mention above.

Next step is to configure matching master data in Ka-ching.

## Manual data setup in Ka-ching

This section assumes you have a Ka-ching account. Get in touch using the contact details found [here](https://ka-ching.dk/about-us/) if you need an account.

Go to [Ka-ching Backoffice](https://backoffice.ka-ching.dk/login) to manage the manual data maintenance listed below. 

More info on how to use the Ka-ching Backoffice can be found on our [Zendesk site](https://ka-ching.zendesk.com).

* Create default taxes that will be applied to all products.
* Create markets with ids that match the market ids used in Episerver. Select appropriate default taxes for the new markets.
* Create and configure shops. Select appropriate market for each of the shops.
* Create cashiers for each shop.
* Create and invite the appropriate users needed. Ususally a shop manager for each shop is enough.
* If you use custom product attributes you'll have to define them manually with ids matching the `attributeId` specified in the web.config configuration in Episerver.
* If you use related products or recommendations as they're called in Ka-ching then you have to configure recommendation categories manually where the ids match the association group ids in Episerver.

That should do it.

## Data from Episerver to Ka-ching

### Products

 * Product code
 * Display name for all defined languages.
 * Default prices for all markets.
 * First image present if any.
 * All parent categories are added as tags to enable folder structure in Ka-ching.
 * Barcode and description fields if they are configured.
 * Any custom attribute fields that are configured.

### Variations

 * Variant code
 * Display name for all defined languages.
 * Default prices for all markets.
 * First image present if any.
 * Barcode field if it's configured.
 * Any custom attribute fields that are configured.

### Categories

 * Display name for all defined languages.

### Product associations

* Associated product codes for each product

### Product assets

* Links for PDF documents with name for all defined languages.
* Links for JPG and PNG pictures with name for all defined languages.

### Assumptions and limitations
This solution assumes the following:

- Products only exist 1 place in the category structure. That is that there are no duplicates of products with the same `Code` - Ka-ching can handle a product being placed multiple places in a folder structure, but the Add-On currently doesn't have the logic to handle it.
- Products only have variations as children.
- Products only have categories as parents.
- Variations only have products as parents.
- Categories only have categories as parents.
- Only 1 catalog.

We might be able to remove some or all of those in future versions.

### Note on prices

Ka-ching prices are including VAT style taxes and excluding "sales tax" style taxes. This solution assumes that the value in IPriceAmount.UnitPrice.Amount uses the same semantics.

### Note on codes

Ka-ching has a limitation on certain characters in ids so the Add-On sanitizes the codes on Episerver models the way out of Episerver and desanitizes on the way back in. For example a product code of ".123123" would be turned into a product id of "%2E123123" in Ka-ching. When data from a checkout in Ka-ching is processed in Episerver the product id from Ka-ching "%2E123123" is turned back into ".123123". Restricted characters are the following: ".", "$", "#", "[", "]", "/", "*" plus ASCII control characters (0x00-0x1F and 0x7F).

### Variants in Ka-ching POS

Ka-ching has a concept of connecting variants through dimensions and dimension values. It can be used as a structured way of selecting variants in the POS app. A dimension could for example be `Size` and that dimension could have values `Small`, `Medium` and `Large`. This version doesn't have any way of configuring product dimensions and which custom attribute in Episerver to use as valsues.

Instead you will see a list off all variants in the product details page in POS. To be able to distinguish between there are a couple of ways to go:

- Use a custom attribute which shows the defining property of the variant.
- Put the value in the name of the variant.

### Specialized taxes per product

Ka-ching is capable of handling custom taxes on a per product basis that override the default taxes specified for a particular market, but the Add-On doesn't look for custom taxes in Episerver.

### Bundles and packages

Ka-ching doesn't support product bundles or packages out of the box, so these have been skipped in this version. It might be added in a future version.

### Handled events

* CatalogContentUpdated
  * AssociationDeletedEventType
  * AssociationUpdatedEventType
  * CatalogEntryDeletedEventType
  * CatalogEntryUpdatedEventType
  * CatalogNodeUpdatedEventType
  * CatalogNodeDeletedEventType
  * RelationUpdatedEventType 
* CatalogKeyEventUpdated
  * PriceUpdate

## Customer search

Customers are found by `CustomerContext.Current.GetContactsByPattern(q)` and the results are then converted into Ka-ching format. The following fields are used:

* PrimaryKeyId
* FullName
* Email (if not found on address)
* ContactAddresses (first shipping address)

## Checkouts in Ka-ching

By default if a sale export web hook is configured in Ka-ching, data from all checkouts are passed on the receiver. 

The Add-On currently filters out data that represents returns and voided sales. Voided sales are sales that have at least one successful payment, but are then aborted.

For normal sales the Add-On creates orders in Episerver where the order shipment status is set to `Shipped`.

### Ecommerce order from store

If the feature is enabled, Ka-ching makes possible for the store associates to sell items from the webshop with the expectation that the webshop will handle fulfillment of that order. It's useful if the store doesn't carry the full product catalogue or they are temporarily sold out of a particular item that the customer wants.

This kind of sale in Ka-ching will have a special line item that represents the shipping information, like delivery address, id of shipping method and costs. If such a line is present the Add-On will create a separate shipment with status `AwaitingInventory` and the order itself will be marked as `InProgress`.

Shipping methods are currently manually defined in Ka-ching with flat fees. At this point we only try to parse the method id as a GUID and if it succeeds it's assigned to `ShippingMethodId` of the shipment.

### Special cases

* Ka-ching uses marks certain line items to represent special products that are connected to special behaviors. For now all those line items, except in the case of shipping as mentioned above, are ignored when creating orders in Episerver. These are the special products that exists currently:
  * Giftcard and voucher purchase
  * Giftcard and voucher use (if the giftcard or voucher is taxed at purchase)
  * Expenses
  * Customer account deposit
  * Container deposit

## Logging in host project

In the Foundation solution go to the EPiServerLog.config file in src\Foundation and add these lines inside the `<log4net>` tag.

```
<logger name="KachingPlugIn">
   <level value="Info" />
   <appender-ref ref="debugLogAppender" />
</logger>
```

If there's no `debugLogAppender` appender defined then paste this element inside the `<log4net>` tag.

```xml
<appender name="debugLogAppender" type="log4net.Appender.RollingFileAppender" >
	<file value="..\appdata\SiteDebug.log" />
	<encoding value="utf-8" />
	<staticLogFileName value="true"/>
	<datePattern value=".yyyyMMdd.'log'" />
	<rollingStyle value="Date" />
	<lockingModel type="log4net.Appender.FileAppender+MinimalLock" />
	<appendToFile value="true" />
	<layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %level %logger: %message%n" />
	</layout>
</appender>
```

## Building and installing locally

### Building

- Clone or download this repo.
- Open KachingPlugIn.sln in Visual Studio 2017.
- Go to the bottom of the file Main.targets in the Packager folder and change the value of `DestinationFolder` in this tag `<Copy  SourceFiles="@(_CopyItems)" DestinationFolder="C:\Users\User\nugets" />`to match the path of your local nuget folder.
- Ctrl-Shift-B to build solution. The package dependencies should be downloaded automatically based on the packages.config file.

### Running

- Open your favorite Episerver Commerce project.
- Make sure you have a NuGet feed that's looking in the folder specified above.
- In the Package Manager Console run the command `install-package KachingPlugIn`.
- Build solution.
- Refresh your browser or debug solution if IIS Express is not already running.

## Known issues

* We're experiencing that changes to images doesn't generate a change event in Episerver and as of this point we haven't found a way around it.

## Ideas for future versions

- Handling of tax data per product.
- Bundles and packages.
- Synchronization of stock values to Ka-ching.
- Synchronization of campaigns.
- Synchronization of markets and default taxes.
- Better marking and handling of click & collect orders.