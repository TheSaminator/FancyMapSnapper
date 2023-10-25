# Changelog

## v1.1.1

* Searching for a place by-address no longer uses its name if a name field is
  present; for example, this prevents queries for a chain restaurant at a
  specific address from returning every single place with the same name as that
  restaurant. Instead, by-address searching now generates an ID-query in all
  cases.

## v1.1.0

* Implement saving and loading map data
* Implement searching for places by-address as well as by-name

## v1.0.0

Initial release
