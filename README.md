Time.txt
========

A simple text file to track your time. Inspired by [todo.txt](http://todotxt.com/).

Currently, time.txt is mostly just a set of conventions for tracking your time
in a text file. There is also an optional background application and syntax
highlighting for a couple of editors. More functionality will be implemented
gradually as I find time for it.

Format
------

Unlike todo.txt, your timesheet is at least somewhat heirarchical. You likely
track your time on a weekly, bi-weekly, or monthly basis - depending on
billing/invoicing requirements.

The bare minimum syntax looks something like this:

	1/11
	8:11, 8:35, Task 1

	1/12
	8:50, 9, Task 2

A blank line separates each block of tasks for a given day. The first line is
the date (in whatever format you prefer), and each line that follows begins
with the start time of the task followed by comma, then the end time followed
by comma (unless the task is ongoing), and finally an optional description.

The description can contain any arbitrary text, but there are some conventions
that you can follow which the supported editors can highlight for you
(more on that later).

Tools
-----

There is an available application that runs in the background (Windows-only) that
will make various changes to your time.txt file whenever you save it, including: 
inferring from shorthand and expanding date and time values, performaing duration
calculations, adding day and week totals, and adding padding to improve readibility.

**Before:**

	7/21
	8:11, 8:35, Catch up on email
	8:50, 9, Support-Corporate: [CorpWeb] Investigate users' login problems.
	9, 11:52, Weekly team meeting
	1:04, 1:39, Maint-Authentication: [CorpWeb] #459 - Fix login problem in browser X.
	1:39, 1:45, Feature-UserPrefs: Start work on initial prototype.
	2:02, 3:09, Training for new team member (@Bob).

**After:**

	Monday, July 21, 2014
	=====================
	(0:24)  8:11a,  8:35a,  Catch up on email
	(0:10)  8:50a,  9a,     Support-Corporate: [CorpWeb] Investigate login problems.
	(2:52)  9a,     11:52a, Weekly team meeting
	(0:35)  1:04p,  1:39p,  Maint-Auth: [CorpWeb] #459 - Fix login problem in browser X.
	(0:06)  1:39p,  1:45p,  Feature-UserPrefs: Start work on initial prototype.
	(1:07)  2:02p,  3:09p,  Training for new team member (@Bob).
	
	Day: 5:14
	
	Week: 5:14


### Install with [Chocolatey](https://chocolatey.org/ "Chocolatey")

	choco install time.txt.install -Source https://www.myget.org/F/mattheyan-chocolatey/api/v2

NOTE: Eventually this should be on the main Chocolatey package feed. For now this is the best way to get the latest update as soon as possible. 


### Syntax Highlighting

Syntax highlighting is supported in two editors currently: Notepad++ and Vim.
Due to differences in the editors the syntax will look a little different, but
this is approximately what it should look like in each.

Vim (gVim using the 'wombat' color scheme):

![Screenshot in Vim](https://raw.github.com/mattheyan/time.txt/master/syntax-vim.png)

Notepad++:

![Screenshot in Notepad++](https://raw.github.com/mattheyan/time.txt/master/syntax-npp.png)

The following are highlighted currently:

* Per-day duration at the beginning of each line.
	- This was done in order to more easily distinguish from the start and end times.
* Project names that are surrounded by `[` and `]`.
	- If you work on multiple projects concurrently, you may find it useful to
	  search or visually scan by project.
* Issue or task numbers that are prefixed with `#`.
	- I find this useful in order to easily highlight all tasks for a particular
	  body of work, and also to draw attention to the fact that sometime I'm
	  working on is or is not tracked by an issue or task.  
* Names that are prefixed with `@`.
	- I find this useful when reviewing time in hindsight in order to see who I've
	  been working with at a glance.
* Task category labels that take the form: `TYPE-name:`.
	- <font style='color:darkgreen;font-weight:bold;'>Feature</font> work is highlighted green (green=good) since it theoretically
	  could generate more business or revenue.
	- <font style='color:darkorange;font-weight:bold;'>Maintenance</font> tasks are highlighted orange as a warning since ideally you
	  shouldn't have to invest too much time in maintaining a feature that is
	  already complete.
	- <font style='color:red;font-weight:bold;'>Support</font> tasks are highlighted red because they generally don't add value
	  (to the software at least), and if you're spending too much time on support
	  it may be indicative of a problem (unless that's your job description).   

### Installation

You can find the syntax files in the 'Editors' folder.

Roadmap
-------

### Reporting

* Every week I have to take the time that I've recorded and enter it into an
internal system. I'd like to implement a reporting tool to automate this somewhat.
* I would like to have other ways to interact with my time file, e.g. a CLI, and possibly even a web interface for things like reporting.


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
