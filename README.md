Time.txt
========

A simple text file to track your time.  Inspired by [todo.txt](http://todotxt.com/).

Currently the main focus is transforming shorthand into more descriptive time tracking information; including inferred dates and times, duration calculations, and day and week totals.

This:

	7/21
	8:11, 8:35, Read hacker news
	8:50, 9, Go for a walk
	9, 11:52, Meetings. ZZZzzzz...
	1:04, 1:39, Nap after lunch
	1:39, 1:45, Check emailz
	2:02, 3:09, Cat videos!!!

...becomes:

	Monday, July 21, 2014
	=====================
	(0:24)  8:11a,  8:35a,  Read hacker news
	(0:10)  8:50a,  9a,     Go for a walk
	(2:52)  9a,     11:52a, Meetings. ZZZzzzz...
	(0:35)  1:04p,  1:39p,  Nap after lunch
	(0:06)  1:39p,  1:45p,  Check emailz
	(1:07)  2:02p,  3:09p,  Cat videos!!!

	Day: 5:14

	Week: 5:14

### Future plans

* Aggregate duration segments into buckets or categories.
* Optional command-line, GUI, or web interfaces.


Installation
------------

### Install with [Chocolatey](https://chocolatey.org/ "Chocolatey")

	choco install time.txt.install

Development
-----------

Solution and source can be found in the 'Source' directory.

There are a few .bat files available to perform common tasks.

A couple of example commands via PowerShell:

	# Compile the solution (debug configuration) and run tests.
	Invoke-psake .\Scripts\PsakeTasks.ps1 Test

	# Deploy to Program Files (after building and running tests).
	Invoke-psake .\Scripts\PsakeTasks.ps1 Deploy

App Icon: http://commons.wikimedia.org/wiki/File:Current_event_clock.svg

Exit Icon: http://commons.wikimedia.org/wiki/File:Red_x_small.PNG
