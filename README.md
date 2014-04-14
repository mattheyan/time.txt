# Time.txt #

A simple text file to track your time.  Inspired by [todo.txt](http://todotxt.com/).

Currently the main focus is transforming shorthand into more descriptive time tracking information; including inferred dates and times, duration calcuations, and day and week totals.

This:

	1/1
	9, 5, do stuff

...becomes:

	Tuesday, January 01, 2013
	=========================
	(8:00) 9a, 5p, do stuff

	Day: 8:00

	Week: 8:00

## Downloads ##

[Download the latest version](downloads) and start tracking time with ease.

## Development ##

Solution and source can be found in the 'Source' directory.

There are a few .bat files available to perform common tasks.

A couple of example commands via PowerShell:

	# Compile the solution (debug configuration) and run tests.
	Invoke-psake .\Scripts\PsakeTasks.ps1 Test

	# Deploy to Program Files (after building and running tests).
	Invoke-psake .\Scripts\PsakeTasks.ps1 Deploy

App Icon: http://commons.wikimedia.org/wiki/File:Current_event_clock.svg
Exit Icon: http://commons.wikimedia.org/wiki/File:Red_x_small.PNG
