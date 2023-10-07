# Fancy Map Snapper

A tool that makes useful maps of town layouts!

## Crunch

Using [OpenStreetMap](https://openstreetmap.org/) data from the Overpass API,
Fancy Map Snapper will draw out roads, buildings and everything in between,
write labels on them, and render an image that you can export!

### How to use?

[Check out the enclosed instruction book](HELP.txt)

### Features to implement later

* [ ] Resizable window
* [ ] Customizing:
  * [ ] Object colors
  * [ ] Line thickness
  * [ ] Text size
  * [ ] Image size
* [x] Saving map data so it can be opened again
* [x] Searching for places by-address instead of by-name

## Fluff

I made Fancy Map Snapper for a family member who works for a company related to
University Hospitals. She needed a map of the area around her workplace, so her
customers could know how to get to the UH centers from wherever they are.

At first, this family member was about to borrow my sister's drawing tablet so
she could draw it herself, but then she asked me if there was anything I could
do to help. She thought I was going to draw the map manually, but then I
started talking about making a program to create the map based on a series of
parameters, which she thought was an even better solution!

And so my answer became Fancy Map Snapper.

### Origin of the UI code

Obviously I wasn't going to implement all of this fancy UI code just for a
map-making program. Instead, I simply took my pre-existing UI code from one of
my earlier projects, and shoved it into this codebase.
