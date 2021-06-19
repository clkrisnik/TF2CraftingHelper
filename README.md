# TF2CraftingHelper

Weapon crafting in TF2 is known to be profitable, given a huge sample size, but it's generally considered to be not worth the time spent on the menial task of individually clicking each weapon, and the associated arm strain.  This automates the process to some extent, you look at where the items you want to craft are in your inventory, enter it into the program, and then let it go to work mulching all of them.

## Limitations

* Some problems that properly selecting and decrementing aren't possible to resolve without some kind of OCR or mapping out 18 additional coordinates for the blue text that denotes the x in the top left corner (by the way, this would break for multiple stacks of 1, both not bearing an X).

* This is currently made for a 1280x600 resolution.  I have tried to make this a function of ResolutionX and ResolutionY, but I cannot guarantee that anything that isn't 1280x600 will have clicks in the proper locations.

* This is limited by your processor speed, you can configure its delay by changing the Thread.Sleep argument in induceclick().  I'm capable of running it with a 125ms delay on a budget gaming laptop from 2016, so I'd generally be inclined to say you can run it faster.  I would generally recommend not running this when anything you care about is craftable, just in case of a data entry issue on your part, but especially not when it's in either R1c1 or R1c2, as these correspond to menu items and can easily be mulched if you aren't careful.  This especially goes if you're trying to figure out what you want for your delay.

* Additionally, the speed at which inventory loads is influenced by your backpack size.  I get desyncs if my backpack size goes over 750, it can be thinned out just by exchanging a ref stockpile for keys.

* Occasionally, if you have an item that's stacked alongside another identical one that's in a different stack for some reason (e.g. contract reward, crafted text, gifted text), the order of these 2 stacks is essentially random.  This does not apply to achievement items, they will be in the same stack as any other craft wep.  If they're on your last page, they won't get mulched.  However, I would enter these last (or just get non-craftable versions of any unique weapon you want to keep, it's only a scrap per)

* This stacking issue is relevant with respect to wildcards, if the wildcard item deducts from a large stack expecting a stack of 1, the slot will be wrong, and vice versa.  Manually crafting these gifted/crafted items doesn't take much time, minimizes the chance of a data entry error, and minimizes the chance of a wrong stack deduction error.

## Known issues

* Wildcard deduction is a bit of a mess.  If a stack is finished off as the third entry where the first 2 are wildcards, it will not properly decrement the slot of every remaining weapon.  Decrementing count of the stack works, though, so I would recommend, right after a wildcard entry of n, try entering a stack of at least 1/2n for a non-wildcard entry.

* If you're crafting a number of weapons for class tokens that is indivisible by 3, the last craft will take 1 or 2 weapons, **deduct the stack entirely**, and move on, despite those weapons not having actually been destroyed.  If there are slot token items after this last class weapon slot, the inventory slot for all of these will be wrong.
