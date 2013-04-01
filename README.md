Composite C1 Azure Manager and Log Viewer.
==============

This is the work of one days coding. Goal was to create a Log Viewer for Composite C1 Azure part that only downloaded the changes written to the logs in the storage account.  

The application have been build using MVVM patterns and should be easy to do further work on.

##Version History
RC 0.1
---
Varius senaries for making the application thrown an exception have been handled and things have been made Async, not frezzing the UI.
Feel free to try build and run it. If it crash, feel free to create an issue telling alittle about what you did :)

Beta 0.1
---
Initial Commit: It shows the log when open connection is pressed for a valid storage account. There are no write backs to the storage account and it should be safe to use on deployment sites.
The goal for next version is to handle various errors that might happen in this version; one example is clicking open connection when the log tap is not active.
The purpose of this first version was only a proof of concept.

Copyrights
==========
Copyright (c) \<2013> \<Poul K. Sørensen>
By installing, copying, or otherwise using this Software, you agree to be bound by the terms of none commercial use only. If you do not agree, do not install copy or use the Software. The Software is protected by copyright and other intellectual property laws and is licensed, not sold.

The software comes “as is”, with no warranties. This means no express, implied or statutory warranty, including without limitation, warranties of merchantability or fitness for a particular purpose, any warranty against interference with your enjoyment of the software or any warranty of title or non-infringement. There is no warranty that this software will fulfill any of your particular purposes or needs. Also, you must pass this disclaimer on whenever you distribute the software or derivative works.

Neither Poul K. Sørensen nor any contributor to the software will be liable for any damages related to the software, including direct, indirect, special, consequential or incidental damages, to the maximum extent the law permits, no matter what legal theory it is based on. Also, you must pass this limitation of liability on whenever you distribute the software or derivative works.




![How does the application look.](https://raw.github.com/s093294/C1AzureManager/master/c1logviewer_gui.png "The Gui")
![Compare Composites Logviewer with mine.](https://raw.github.com/s093294/C1AzureManager/master/c1logviewer.png "Network Usages")


