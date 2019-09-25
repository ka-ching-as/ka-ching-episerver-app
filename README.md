# Ka-ching Episerver Add-On

![](https://raw.githubusercontent.com/ka-ching-as/ka-ching-episerver-app/master/Images/EpiserverKachingPlugIn.png)

## Concept

TODO

[Ka-ching](https://ka-ching.dk)

## Handled data

### Products

 * Display name for all defined languages
 * Prices for all markets
 * First image present if any
 * All parent categories are added as tags to enable folder structure in Ka-ching

### Variations

 * Display name for all defined languages
 * Prices for all markets
 * First image present if any

### Categories

 * Display name for all defined languages

## Assumptions and limitations
This solution assumes the following:

- Products only exist 1 place in the category structure. That is that there are no duplicates of products with the same `Code`
- Products only have variations as children.
- Products only have categories as parents.
- Variations only have products as parents.
- Categories only have categories as parents.

We might be able to remove some or all of those in future versions.

### Custom properties

This solution also doesn't handle any custom properties defined on products, variations or categories. There are some comments in the code to show how size, color and description, as defined in Quicksilver, could be handled.

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
- Synchronization of stock values to Ka-ching.
- Synchronization of campaigns.
