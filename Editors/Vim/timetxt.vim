" Vim syntax file
" Language: time.txt for developers
" Maintainer: Bryan Matthews
" Latest Revision: 25 Aug 2015
" http://vim.wikia.com/wiki/Creating_your_own_syntax_files

if exists("b:current_syntax")
  finish
endif

" http://vim.wikia.com/wiki/Maximize_or_set_initial_window_size
set lines=27
set columns=129

let b:current_syntax = "timetxt"

" Don't wrap text, since lines are significant
set nowrap

syn match lineComment "^#.*$"
syn match taskDuration "(\(1|2\)\?\d:[012345]\d)"
syn match projectName " \[[^\s\]]\+\] "
syn match cardNumberRef "\s#\d\+\s"
syn match personRef "\s@[A-Z][a-z']\+\($\|[^A-Za-z']\@=\)"

syn match timeLabelFeature " Feature\-[A-Za-z0-9]\+:"
syn match timeLabelMaintenance " Maint\-[A-Za-z0-9]\+:"
syn match timeLabelSupport " Support\-[^:]\+:"
syn match timeLabelTool " Tool\-[A-Za-z0-9]\+:"

hi def styleAlert guifg=firebrick
hi def styleAlertSubdued guifg=OrangeRed
hi def styleOkHeavy guifg=green4
hi def styleOkAttention guifg=MediumPurple
hi def styleOkSubduedHeavy guifg=SteelBlue3
hi def styleEmphasis guifg=DarkGray gui=italic

hi def styleOkSubdued guifg=turquoise3
hi def styleOkLight guifg=green3
hi def styleIgnored guifg=DarkSlateGray
hi def styleGreat guifg=goldenrod gui=italic

hi def link lineComment				Comment
hi def link taskDuration			String
hi def link projectName             styleEmphasis
hi def link cardNumberRef			styleGreat
hi def link personRef		    	styleOkSubduedHeavy

hi def link timeLabelFeature		styleOkHeavy
hi def link timeLabelMaintenance	styleAlertSubdued
hi def link timeLabelSupport	    styleAlert
hi def link timeLabelTool		    styleOkAttention
