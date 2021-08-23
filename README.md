# TF2CraftingHelper

Weapon crafting in TF2 is known to be profitable, given a huge sample size, but it's generally considered to be not worth the time spent on the menial task of individually clicking each weapon, and not worth the arm strain of repeatedly moving the mouse to the right spot over and over.  This automates the process to some extent, you look at where the items you want to craft are in your inventory, enter it into the program, and then let it go to work mulching all of them.

## How to use

1. Launch TF2.  Go to the crafting menu, select either "Fabricate Class Token" or "Fabricate Slot Token", click the first slot, and note the location of the items you wish to craft.

2. The rows and cols are base 0, and page 2 r0c1 would be noted as r3c1.

![row and col guides](https://i.imgur.com/W3TnrRC.png)

3. Enter each class token part into the prompt, in order of row, column, quantity.  In the case of a wildcard item (e.g. reserve shooter), you would note that as 1, 1, 1w.  Once you are done, put something that isn't a number (besides the letter "w", which is used for wildcard resolution) into the row prompt.  It will then prompt you for slot tokens in the same order, once done, put in a non-number in the row prompt.  Press any key, go back into your tf2 window, and wait a bit.  I always liked putting up the fabricate class weapons menu once or twice while I waited, the first one seems to take ever-so-slightly longer.

4. Watch the tokens get created, watch ref get smelted (meditate for a moment on the fact that this is the most profitable way to destroy ref, and this does so incredibly slowly), and watch the tokens get crafted, if you get even below-average luck, you will likely profit.

## Limitations

* This is currently made for a 1280x600 resolution.  I have tried to make this a function of ResolutionX and ResolutionY, but I cannot guarantee that anything that isn't 1280x600 will have clicks in the proper locations.

* This is limited by your processor speed, you can configure its delay by using the -d command line argument.  It can be run comfortably on a 125ms delay on a budget gaming laptop from 2016, so I'd generally be inclined to say you can run it faster.  

* I would generally recommend not running this when anything you care about is craftable, just in case of a data entry issue on your part, but especially not when it's in either R0c1 or R0c2, as these correspond to menu items and can easily be mulched if you aren't careful.  This especially goes if you're trying to figure out what you want for your delay.

* Additionally, the speed at which inventory loads is influenced by your backpack size.  I get desyncs if my backpack size goes over 750, it can be thinned out just by exchanging a ref stockpile for keys.

* Occasionally, if you have an item that's stacked alongside another identical one that's in a different stack for some reason (e.g. contract reward, crafted text, gifted text), the order of these 2 stacks is essentially random.  This does not apply to achievement items, they will be in the same stack as any other craft wep.  If they're on your last page, they won't get mulched.  However, I would enter these last (or just get non-craftable versions of any unique weapon you want to keep, it's only a scrap per).  This stacking issue is relevant with respect to wildcards, if the wildcard item deducts from a large stack expecting a stack of 1, the slot will be wrong, and vice versa.  Manually crafting these gifted/crafted items doesn't take much time, minimizes the chance of a data entry error, and minimizes the chance of a wrong stack deduction error.

## Command-line arguments

* -c/-craft [X] will skip token crafting and metal smelting, and immediately go to fabricate class weapons, and do it X times.
* -s/-smelt [X] will skip token crafting, and immediately go to metal smelting then fabricate class weapons, smelting X metal, and crafting X\*9 tokens.
* Combining -c and -s will skip token crafting, and smelt the -s argument's amount of metal, and craft the -c argument's amount of tokens.
* -d/-delay [X] will configure the delay between clicks, in milliseconds.
* help will display a guide to these arguments.

## Known issues

* If you're crafting a number of weapons for class tokens that is indivisible by 3, the last craft will take 1 or 2 weapons, **deduct the stack entirely**, and move on, despite those weapons not having actually been destroyed.  If there are slot token items after this last class weapon slot, the inventory slot for all of these will be wrong.

## To do

* Maybe make a flag that will allow 4 arguments per item, position and quantity like usual, as well as stack count, which can help if you picked up 20+ crafted items each with their own individual stack, rather than entering all of them as r c 1, or manually crafting them first.
* Create a mode that utilizes Rebuild Class/Slot token, 2 prompts for class/slot token each, also will ask for the alphabetical order of these class spots relative to the other (necessary to get the spots right)
