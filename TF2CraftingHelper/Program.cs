using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;


namespace TF2CraftingHelper
{

    class InventoryItem {
        public int slot;//the crafting menu goes in a 3x6 grid, then a next and previous page button.  page 1 r3c4 would be 16 in this.  a page 2 r1c3 would be 21
                        //you know what?  i don't want to do multiplications of 6.  i think it's easier to count rows by 1 rather than multiplying by 6.
                        //i want to minimize the chance of mulching my entire inventory i think!
        public int quantity;//self-explanatory
        public bool wildcard;
    }

    class RebuildInstructions {
        public bool order;//false means that the input token (e.g. class token - demoman) is before the desired token (e.g. class token - scout) in the alphabet
        //this needs to be done because you could, say, recraft soldier stuff into scout stuff, where scout is before soldier in the alphabet, so it'll display the other way.
        public List<InventoryItem> items;
    }
   
    class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;

        //i possess a 1920x1080 monitor, so i'll try to express x and y coords as functions of your resolution.  might want to use this code later on a new comp or something
        //cannot guarantee the function of this program at any other resolution.  you know where you want it to go, you're smart, you can work backwards
        public const int ResolutionX = 1280;
        public const int ResolutionY = 600;

        public static int delay = 125;//time in ms between each click.  can be configured with the -d/-delay flag

        static void Main(string[] args)
        {
            //i do believe i need an indefinite amount of arguments, which means i'll probably just call this thing, and there'll be some prompts to actually get them
            //it'll go like:
            //"put the inventory slot and quantity of the item you wish to be class token'd, type C to continue to the slot token stage"
            //each individual entry will be pushed onto an array of inventory slots
            //similar such prompt for slot
            //note that these will necessarily change.  actually, you know, maybe not? 
            //maybe i can do the opposite of what i do as a human (start in front, as front items are destroyed, first page will contain more craftable items)
            //wait no.  this will not work unless i transition between class and slot regularly.  i do not want to do this.  it's easier to start in front and decrement each class/slot's slot upon destroying a stack of craft weps.
            List<InventoryItem> classtokens = new List<InventoryItem> { };
            List<InventoryItem> slottokens = new List<InventoryItem> { };
            RebuildInstructions rebuildclasstokens = new RebuildInstructions { order = false, items=new List<InventoryItem> { } };
            RebuildInstructions rebuildslottokens = new RebuildInstructions { order = false, items=new List<InventoryItem> { } };
            double test = 0.0;//never actually used by anything other than tryparse
            string slotrow = "1";
            string slotcol = "1";
            string qty = "1";//need string so i can detect a non-number, so i can stop gettign stuff there
            int totaltokens = 0;
            bool rebuildflag = false;
            bool wildcardflag = false;
            int wildcardct = 0;
            int nonwildcardct = 0;
            int refct = 0;
            int i = 0;//will have to be done for ref counts.
                      //to get to the center of the top left one, it'd be about 337 length 195 height
                      //to get to the center of the next one on the top it'd be about 456 length
                      //can extrapolate that for each row, for each col...
                      //for the second row, it's 288 height
                      //as a ratio of my resolution, it'd be:
                      //0.26328125 of resolution length to the first one, increments by 0.09296875L
            List<int> rpos = new List<int> { (int)(.325 * ResolutionY), (int)(.48 * ResolutionY), (int)(.635 * ResolutionY) };
            List<int> cpos = new List<int> { (int)(.2633 * ResolutionX), (int)(.3563 * ResolutionX), (int)(.4492 * ResolutionX), (int)(.5508 * ResolutionX), (int)(.644531 * ResolutionX), (int)(.7320 * ResolutionX) };
            //List<int> cpos = new List<int> { 402, 515, 636, 767, 881, 999 };
            //683 210 for the first
            //770 210 for the second
            //873 210 for the third
            List<int> craftingmenucolpos = new List<int> { (int)(.5336 * ResolutionX), (int)(.6016 * ResolutionX), (int)(.6820 * ResolutionX) };
            int craftingheight = (int)(.35 * ResolutionY);
            //763 496 for crafting button coords
            Tuple<int, int> craftbuttoncoords = Tuple.Create<int, int>((int)(.5961 * ResolutionX), (int)(.8267 * ResolutionY));
            //639 359 for the ok button after crafting succeeds
            Tuple<int, int> okbuttoncoords = Tuple.Create<int, int>((int)(.4992 * ResolutionX), (int)(.5983 * ResolutionY));
            //865 542 for the continue button after it shows you the results
            Tuple<int, int> continuebuttoncoords = Tuple.Create<int, int>((int)(.6758 * ResolutionX), (int)(.9033 * ResolutionY));
            //next button is at 982 length 453 height
            Tuple<int, int> nextbuttoncoords = Tuple.Create<int, int>((int)(.7672 * ResolutionX), (int)(.755 * ResolutionY));

            //first of all, let's see if i put in any args.
            //-help shows a help menu related to args
            //-s [X] immediately skips to smelting ref, smelts x amount of ref and gets to crafting the requisite amount of tokens (may have to alt-tab out of it, can be up to 8 excess)
            //-c [X] immediately skips to crafting tokens, crafts x tokens, assumes scrap metal and the tokens are already there
            //-d [X] will change the delay between clicks, the thread.sleep arg in 
            if (args.Length>0)
            {
                //for the purpose of implementing quickly, manually writing out the potential orders for these flags is fine, but there's a couple args to add on the todo list, so perhaps an order-agonostic function is in... order, if you will
                int smelti = -1, crafti=-1, delayi=-1;
                bool helpflag=false;
                List<string> argtext = new List<string> { };
                List<string> argvals = new List<string> { };
                for (i = 0; i < args.Length; i++)
                {
                    if (args[i] == "help" || args[i] == "-help" || args[i] == "--help")
                    {
                        helpflag = true;
                    }

                    if (args[i] == "-r" || args[i] == "-recraft")
                    {
                        rebuildflag = true;
                    }

                    if (i % 2 == 0)
                    {
                        if (args[i] == "-s" || args[i] == "-smelt")
                        {
                            smelti = i/2;
                        }
                        else if (args[i] == "-c" || args[i] == "-craft")
                        {
                            crafti=i/2;
                        }
                        else if (args[i] == "-d" || args[i] == "-delay")
                        {
                            delayi = i/2;
                        }
                        
                        argtext.Add(args[i]);
                    }
                    else {
                        argvals.Add(args[i]);
                    }
                }
                if (helpflag)
                {
                    Console.WriteLine("Arguments for TF2CraftingHelper:");
                    Console.WriteLine("-r/-recraft X will add a prompt, the first prompts will use Fabricate Token like usual, then another prompt will show that will be for the weapons used in Rebuild Token.");
                    Console.WriteLine("-s/-smelt X will skip token prompting and smelt X metal, and attempt to craft X*9 tokens.");
                    Console.WriteLine("-c/-craft X will skip token prompting and metal smelting and attempt to craft X tokens.");
                    Console.WriteLine("Combining -s and -c will skip token prompting, smelt the metal specified in the -s argument, then craft the tokens specified in the -c argument.");
                    Console.WriteLine("-d/-delay X will run the program with a delay of X ms between clicks.  The default delay is 125.");
                    Console.WriteLine("help/-help will show this menu.");
                    return;
                }
                else
                {

                    if (crafti != -1 && smelti != -1)
                    {
                        slotrow = null;
                        totaltokens = Convert.ToInt32(argvals[crafti]);
                        refct = Convert.ToInt32(argvals[smelti]);
                    }
                    else if (crafti != -1)
                    {
                        slotrow = null;
                        totaltokens = Convert.ToInt32(argvals[crafti]);
                        refct = 0;
                    }
                    else if (smelti != -1)
                    {
                        slotrow = null;
                        refct = Convert.ToInt32(argvals[smelti]);
                        totaltokens = refct * 9;
                    }

                    if (delayi != -1)
                    {
                        delay = Convert.ToInt32(argvals[delayi]);
                    }

                }

            }


            while (Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
            {
                wildcardflag = false;
                //class prompt
                Console.WriteLine("Put in your row for a class token item.");
                slotrow = Console.ReadLine();
                if (Double.TryParse(slotrow, out test))
                {
                    Console.WriteLine("Put in your column for this class token item.");
                    slotcol = Console.ReadLine();
                    Console.WriteLine("Put in your quantity for this item.");
                    qty = Console.ReadLine();
                    if (Char.ToUpper(qty.Last()) == 'W')
                    {
                        qty = qty.Replace("W", "").Replace("w","");
                        wildcardflag = true;
                    }
                    if (Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
                    {
                        //these are all numeric values, which means that it's on.  insert this guy.
                        classtokens.Add(new InventoryItem { slot = 6 * Convert.ToInt32(slotrow) + Convert.ToInt32(slotcol), quantity = Convert.ToInt32(qty), wildcard = wildcardflag });
                        if (wildcardflag)
                        {
                            wildcardct += Convert.ToInt32(qty);
                        }
                        else {
                            nonwildcardct += Convert.ToInt32(qty);
                        }
                    }
                }
            }

            //the class token items are entered.  time to get a slot token prompt.  it's about the same.
            //notable that while there are indeed wildcards in terms of slots, most notably the panic attack, it's also true that
            //scrap.tf seems to never have a serious amount of them in stock, which is the whole reason i implemented wildcard detection for the reserve shooter
            if (!String.IsNullOrEmpty(slotrow))
            {
                slotrow = "1";
                slotcol = "1";
                qty = "1";
            }
            while (Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
            {

                //slot prompt
                Console.WriteLine("Put in your row for a slot token item.");
                slotrow = Console.ReadLine();
                if (Double.TryParse(slotrow, out test))
                {
                    Console.WriteLine("Put in your column for this slot token item.");
                    slotcol = Console.ReadLine();
                    Console.WriteLine("Put in your quantity for this item.");
                    qty = Console.ReadLine();
                    if (Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
                    {
                        //these are all numeric values, which means that it's on.  insert this guy.
                        slottokens.Add(new InventoryItem { slot = 6 * Convert.ToInt32(slotrow) + Convert.ToInt32(slotcol), quantity = Convert.ToInt32(qty) });
                    }
                }
            }
            if (!String.IsNullOrEmpty(slotrow))
            {
                totaltokens = Math.Max(slottokens.Sum(s => s.quantity) / 3, classtokens.Sum(c => c.quantity) / 3);
                refct = (int)Math.Ceiling(Math.Max(slottokens.Sum(s => s.quantity) / 27.0, classtokens.Sum(c => c.quantity) / 27.0));
            }

            //time for potential rebuild candidates.  similar in some respects to the original prompt, it'll go in another order, the important stuff is alphabetical order
            if (rebuildflag)
            {
                slotrow = "1";
                slotcol = "1";
                qty = "1";
                char shouldcontinue= ' ';
                //okay, so, i feel like in the spirit of crossing my t's and dotting my i's, i will still prompt for order on the slot part, just suggest that, in all likelihood your input is secondary, and so is going to be later than anything else
                Console.WriteLine("Is your desired class before your input class tokens in the alphabet? (y/n) (for example, if you're putting in Soldier class tokens to produce Scout class tokens, you answer y)");
                Console.WriteLine("Answer Z if you aren't recrafting class tokens.");
                shouldcontinue = Console.ReadLine().ToUpper()[0];
                rebuildclasstokens.order =  shouldcontinue== 'Y';

                while (shouldcontinue!='Z'&&Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
                {

                    //slot prompt
                    Console.WriteLine("Put in your row for a class token item.");
                    slotrow = Console.ReadLine();
                    if (Double.TryParse(slotrow, out test))
                    {
                        Console.WriteLine("Put in your column for this class token item.");
                        slotcol = Console.ReadLine();
                        Console.WriteLine("Put in your quantity for this item.");
                        qty = Console.ReadLine();
                        if (Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
                        {
                            //these are all numeric values, which means that it's on.  insert this guy.
                            rebuildclasstokens.items.Add(new InventoryItem { slot = 6 * Convert.ToInt32(slotrow) + Convert.ToInt32(slotcol), quantity = Convert.ToInt32(qty) });
                        }
                    }
                }
                slotrow = "1";
                slotcol = "1";
                qty = "1";
                //okay, so, i feel like in the spirit of crossing my t's and dotting my i's, i will still prompt for order on the slot part, just suggest that, in all likelihood your input is secondary, and so is going to be later than anything else
                Console.WriteLine("Is your desired slot before your input slot tokens in the alphabet? (y/n) (note that the answer, if you're putting in secondary tokens, will always be yes.)");
                Console.WriteLine("Answer Z if you aren't recrafting slot tokens.");
                shouldcontinue = Console.ReadLine().ToUpper()[0];
                rebuildslottokens.order = shouldcontinue == 'Y';
                while (shouldcontinue!='Z'&&Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
                {

                    //slot prompt
                    Console.WriteLine("Put in your row for a slot token item.");
                    slotrow = Console.ReadLine();
                    if (Double.TryParse(slotrow, out test))
                    {
                        Console.WriteLine("Put in your column for this slot token item.");
                        slotcol = Console.ReadLine();
                        Console.WriteLine("Put in your quantity for this item.");
                        qty = Console.ReadLine();
                        if (Double.TryParse(slotrow, out test) && Double.TryParse(slotcol, out test) && Double.TryParse(qty, out test))
                        {
                            //these are all numeric values, which means that it's on.  insert this guy.
                            rebuildslottokens.items.Add(new InventoryItem { slot = 6 * Convert.ToInt32(slotrow) + Convert.ToInt32(slotcol), quantity = Convert.ToInt32(qty) });
                        }
                    }
                }

                totaltokens = Math.Max(totaltokens, Math.Max(rebuildclasstokens.items.Sum(s => s.quantity), rebuildslottokens.items.Sum(s => s.quantity)));
                refct = Math.Max(refct, (int)Math.Ceiling(Math.Max(rebuildclasstokens.items.Sum(s=>s.quantity), rebuildslottokens.items.Sum(s => s.quantity))/9.0));
            }
            
            //okay, so i've got the easy console application prompt stuff knocked out in like 15 minutes.  time for the big boy problem:  determine where:
            //each slot in the grid lies, probably can make this a function of pixels rather than hardcoding in spots for all the 18 slots
            //each next button.  i think i'm going to put in the bare minimum on the paging part, i'm sure there's efficiency in paging that i can get to, but god i don't want to make you put in another prompt. this ship has one setting, and it's forward!
            //next button is at 982 length 453 height
            //the fabricate class/slot token centers are roughly at 358 px length, class and slot are 229 and 247 height, respectively
            //within each of these:
            //683 210 for the first
            //770 210 for the second
            //873 210 for the third
            //obviously not totally accurate, esp the first
            //but i'll use these coords.  they surely aren't all dead center, but i think they will be sufficient such that when you click on them, you will go into their respective menus
            //smelt ref is 348 337
            //smelt rec is 348 319
            //notable that the metal will always be in slot 1 for these, so i can just click r0c1 repeatedly
            //344 137 is the second crafting menu
            //fabricate class weapons should be at 358 175
            //same thing as the metal, repeatedly click r0c1.  it's also the same positions as the create token
            //i want to make sure there's a wait though.  like a second on this last step
            //i like it because it's gambling, not because it's perfectly efficient.
            //anyway, even if you use the args, you're going to have to alt-tab back in. 10 seconds is a bit conservative, but i haven't been too concerned about slimming that part down
            Console.WriteLine("You have 10 seconds upon continuing to launch tf2, in the crafting menu.  If you aren't prepared, do it now and then continue the program.");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
            Thread.Sleep(10000);
            int toprow = 0;
            int topcol = 0;
            int temptoprow = 0;//this will be freely decremented such that i can see the right row.
            if (classtokens.Any())
            { 
                toprow = classtokens.First().slot / 6;
                topcol = classtokens.First().slot % 6;
                while (classtokens.First().quantity <= 0)
                {
                    classtokens.RemoveAt(0);
                    if (classtokens.Any())
                    {
                        toprow = classtokens.First().slot / 6;
                        topcol = classtokens.First().slot % 6;
                    }
                }
                startclass();

                while (classtokens.Any())
                {
                    for (i = 0; (i < 3 && classtokens.Any()); i++)
                    {
                        induceclick(craftingmenucolpos.ElementAt(i), craftingheight);
                        temptoprow = toprow;
                        

                        
                        if (classtokens.First().wildcard == false && nonwildcardct * 2 < wildcardct)
                        {
                            //use wildcard, and deduct wildcard
                            wildcardct--;
                            int savetemptoprow = temptoprow;
                            int savetemptopcol = topcol;

                            temptoprow = classtokens.Where(c => c.wildcard == true).First().slot / 6;
                            topcol = classtokens.Where(c => c.wildcard == true).First().slot % 6;

                            classtokens.First(c => c.wildcard == true).quantity--;

                            while (temptoprow > 2)
                            {
                                induceclick(nextbuttoncoords.Item1, nextbuttoncoords.Item2);
                                temptoprow -= 3;
                                //Console.WriteLine("going to next page");
                            }
                            induceclick(cpos.ElementAt(topcol), rpos.ElementAt(temptoprow));

                            if (classtokens.First(c => c.wildcard == true).quantity <= 0)
                            {

                                deduct(classtokens.Where(c => c.wildcard == true).First().slot, classtokens, slottokens, rebuildclasstokens.items, rebuildslottokens.items);
                                classtokens.Remove(classtokens.Where(c => c.wildcard == true).First());
                                if (classtokens.Any())
                                {
                                    toprow = classtokens.First().slot / 6;
                                    topcol = classtokens.First().slot % 6;
                                }
                            }
                            else
                            {
                                temptoprow = savetemptoprow;
                                topcol = savetemptopcol;
                            }
                        }
                        else if (i == 2 && classtokens.First().wildcard == true && nonwildcardct >= 1 && nonwildcardct * 2 >= wildcardct)
                        {
                            //use first non-wildcard, deduct it
                            nonwildcardct--;
                            int savetemptoprow = temptoprow;
                            int savetemptopcol = topcol;

                            temptoprow = classtokens.Where(c => c.wildcard == false).First().slot / 6;
                            topcol = classtokens.Where(c => c.wildcard == false).First().slot % 6;

                            classtokens.First(c => c.wildcard == false).quantity--;

                            while (temptoprow > 2)
                            {
                                induceclick(nextbuttoncoords.Item1, nextbuttoncoords.Item2);
                                temptoprow -= 3;
                                //Console.WriteLine("going to next page");
                            }
                            induceclick(cpos.ElementAt(topcol), rpos.ElementAt(temptoprow));

                            if (classtokens.First(c => c.wildcard == false).quantity <= 0)
                            {

                                deduct(classtokens.Where(c => c.wildcard == false).First().slot, classtokens, slottokens, rebuildclasstokens.items, rebuildslottokens.items);
                                classtokens.Remove(classtokens.Where(c => c.wildcard == false).First());
                                if (classtokens.Any())
                                {
                                    toprow = classtokens.First().slot / 6;
                                    topcol = classtokens.First().slot % 6;
                                }
                            }
                            else
                            {
                                temptoprow = savetemptoprow;
                                topcol = savetemptopcol;
                            }


                        }
                        else
                        {

                            if (classtokens.First().wildcard == true)
                            {
                                wildcardct--;
                            }
                            else {
                                nonwildcardct--;
                            }

                            while (temptoprow > 2)
                            {
                                induceclick(nextbuttoncoords.Item1, nextbuttoncoords.Item2);
                                temptoprow -= 3;
                                //Console.WriteLine("going to next page");
                            }
                            induceclick(cpos.ElementAt(topcol), rpos.ElementAt(temptoprow));
                            classtokens.First().quantity -= 1;
                            if (classtokens.First().quantity <= 0)
                            {

                                deduct(classtokens.First().slot, classtokens, slottokens, rebuildclasstokens.items, rebuildslottokens.items);
                                classtokens.RemoveAt(0);
                                if (classtokens.Any())
                                {
                                    toprow = classtokens.First().slot / 6;
                                    topcol = classtokens.First().slot % 6;
                                }
                            }
                        }

                    }

                    if (i == 3)
                    {
                        induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                        clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                        Thread.Sleep(125);
                        induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
                        //Console.WriteLine("one craft done");
                    }
                }
            }

            //pretty similar story for slot.  after this, it's time to smelt metal
            if (slottokens.Any())
            {
                toprow = slottokens.First().slot / 6;
                topcol = slottokens.First().slot % 6;
                while (slottokens.First().quantity <= 0)
                {
                    slottokens.RemoveAt(0);
                    if (slottokens.Any())
                    {
                        toprow = slottokens.First().slot / 6;
                        topcol = slottokens.First().slot % 6;
                    }
                }
                startslot();

                while (slottokens.Any())
                {
                    for (i = 0; i < 3 && slottokens.Any(); i++)
                    {
                        induceclick(craftingmenucolpos.ElementAt(i), craftingheight);

                        temptoprow = toprow;
                        while (temptoprow > 2)
                        {
                            induceclick(nextbuttoncoords.Item1, nextbuttoncoords.Item2);
                            temptoprow -= 3;
                            //Console.WriteLine("going to next page");
                        }
                        induceclick(cpos.ElementAt(topcol), rpos.ElementAt(temptoprow));
                        slottokens.First().quantity -= 1;
                        if (slottokens.First().quantity <= 0)
                        {
                            deduct(slottokens.First().slot, classtokens, slottokens, rebuildclasstokens.items, rebuildslottokens.items);
                            slottokens.RemoveAt(0);
                            if (slottokens.Any())
                            {
                                toprow = slottokens.First().slot / 6;
                                topcol = slottokens.First().slot % 6;
                            }
                        }

                    }
                    induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                    clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                    Thread.Sleep(125);
                    induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
                    //Console.WriteLine("one craft done");
                }
            }

            if (rebuildflag)
            {
                bool didacraft = false;
                //first, see if there's any rebuild class.  note that wildcards do not exist here, as you're only using 1 weapon.
                startrebuildclass();
                while (rebuildclasstokens.items.Any())
                {
                    toprow = rebuildclasstokens.items.First().slot / 6;
                    topcol = rebuildclasstokens.items.First().slot % 6;
                    while (rebuildclasstokens.items.First().quantity <= 0)
                    {
                        rebuildclasstokens.items.RemoveAt(0);
                        if (rebuildclasstokens.items.Any())
                        {
                            toprow = rebuildclasstokens.items.First().slot / 6;
                            topcol = rebuildclasstokens.items.First().slot % 6;
                        }
                    }
                    induceclick(craftingmenucolpos.ElementAt(0), craftingheight);
                    //goes weapon, then token
                    temptoprow = toprow;
                    while (temptoprow > 2)
                    {
                        induceclick(nextbuttoncoords.Item1, nextbuttoncoords.Item2);
                        temptoprow -= 3;
                        //Console.WriteLine("going to next page");
                    }
                    induceclick(cpos.ElementAt(topcol), rpos.ElementAt(temptoprow));
                    rebuildclasstokens.items.First().quantity -= 1;
                    if (rebuildclasstokens.items.First().quantity <= 0)
                    {
                        deduct(rebuildclasstokens.items.First().slot, classtokens, slottokens, rebuildclasstokens.items, rebuildslottokens.items);
                        rebuildclasstokens.items.RemoveAt(0);
                        if (rebuildclasstokens.items.Any())
                        {
                            toprow = rebuildclasstokens.items.First().slot / 6;
                            topcol = rebuildclasstokens.items.First().slot % 6;
                        }
                    }
                    induceclick(craftingmenucolpos.ElementAt(1), craftingheight);
                    //this is where i determine if order should be used.  order true means deisred before input, so click 1 on first, and 2 on all others.  order false means always click 1.
                    induceclick(rebuildclasstokens.order && didacraft ? cpos.ElementAt(2) : cpos.ElementAt(1), rpos.ElementAt(0));
                    induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                    clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                    Thread.Sleep(125);
                    induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
                    didacraft = true;
                }
                didacraft = false;
                startrebuildslot();
                while (rebuildslottokens.items.Any())
                {
                    toprow = rebuildslottokens.items.First().slot / 6;
                    topcol = rebuildslottokens.items.First().slot % 6;
                    while (rebuildslottokens.items.First().quantity <= 0)
                    {
                        rebuildslottokens.items.RemoveAt(0);
                        if (rebuildslottokens.items.Any())
                        {
                            toprow = rebuildslottokens.items.First().slot / 6;
                            topcol = rebuildslottokens.items.First().slot % 6;
                        }
                    }
                    induceclick(craftingmenucolpos.ElementAt(0), craftingheight);
                    //goes weapon, then token
                    temptoprow = toprow;
                    while (temptoprow > 2)
                    {
                        induceclick(nextbuttoncoords.Item1, nextbuttoncoords.Item2);
                        temptoprow -= 3;
                        //Console.WriteLine("going to next page");
                    }
                    induceclick(cpos.ElementAt(topcol), rpos.ElementAt(temptoprow));
                    rebuildslottokens.items.First().quantity -= 1;
                    if (rebuildslottokens.items.First().quantity <= 0)
                    {
                        deduct(rebuildslottokens.items.First().slot, classtokens, slottokens, rebuildclasstokens.items, rebuildslottokens.items);
                        rebuildslottokens.items.RemoveAt(0);
                        if (rebuildslottokens.items.Any())
                        {
                            toprow = rebuildslottokens.items.First().slot / 6;
                            topcol = rebuildslottokens.items.First().slot % 6;
                        }
                    }
                    induceclick(craftingmenucolpos.ElementAt(1), craftingheight);
                    //this is where i determine if order should be used.  order true means deisred before input, so click 1 on first, and 2 on all others.  order false means always click 1.
                    induceclick(rebuildslottokens.order && didacraft ? cpos.ElementAt(2) : cpos.ElementAt(1), rpos.ElementAt(0));
                    induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                    clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                    Thread.Sleep(125);
                    induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
                    didacraft = true;
                }

            }

            if (refct > 0)
            {
                startref();
                for (i = 0; i < refct; i++)
                {
                    induceclick(craftingmenucolpos.First(), craftingheight);
                    induceclick(cpos.ElementAt(1), rpos.ElementAt(0));
                    induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                    clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                    Thread.Sleep(125);
                    induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
                }

                refct = refct * 3;
                startrec();
                for (i = 0; i < refct; i++)
                {
                    induceclick(craftingmenucolpos.First(), craftingheight);
                    induceclick(cpos.ElementAt(1), rpos.ElementAt(0));
                    induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                    clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                    Thread.Sleep(125);
                    induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
                }
                //the tokens are created, the metal is scrapped, and now time for gambling.
            }
            startfabclassweps();
            int j = 0;
            for (i = 0; i < totaltokens; i++)
            {
                for (j = 0; j < 3; j++)
                {
                    induceclick(craftingmenucolpos.ElementAt(j), craftingheight);
                    induceclick(cpos.ElementAt(1), rpos.ElementAt(0));

                }
                induceclick(craftbuttoncoords.Item1, craftbuttoncoords.Item2);
                clickokoncraft(okbuttoncoords.Item1, okbuttoncoords.Item2);
                Thread.Sleep(500);//admiring gambling results.  oddly, this wait menu takes the longest.  400 OFTEN works, but can desync once every 50 times or so.
                induceclick(continuebuttoncoords.Item1, continuebuttoncoords.Item2);
            }
            Console.ReadKey();
            return;
        }

        public static void deduct(int slot, List<InventoryItem> classitems, List<InventoryItem> slotitems, List<InventoryItem> rclass, List<InventoryItem> rslot)
        {
            foreach (InventoryItem item in classitems)
            {
                if (item.slot > slot)
                {
                    item.slot -= 1;
                }
            }
            foreach (InventoryItem item in slotitems)
            {
                if (item.slot > slot)
                {
                    item.slot -= 1;
                }
            }
            foreach (InventoryItem item in rclass)
            {
                if (item.slot > slot)
                {
                    item.slot -= 1;
                }
            }
            foreach (InventoryItem item in rslot)
            {
                if (item.slot > slot)
                {
                    item.slot -= 1;
                }
            }
        }

        public static void startclass()
        {
            //clicks the fabricate class token part to get that part ready
            int classy=(int)(0.3817 * ResolutionY);
            int classx=(int)(0.2983 * ResolutionX);
            induceclick(classx, classy);
            return;
        }

        public static void startslot()
        {
            //clicks the fabricate slot token part to get that part ready
            int classy = (int)(0.4117 * ResolutionY);
            int classx = (int)(0.2983 * ResolutionX);
            induceclick(classx, classy);
            return;
        }
        //smelt ref is 348 337
        //smelt rec is 348 319
        public static void startref()
        {
            int classy = (int)(0.5617 * ResolutionY);
            int classx = (int)(0.2719 * ResolutionX);
            induceclick(classx, classy);
            return;
        }

        public static void startrec()
        {
            int classy = (int)(0.5317 * ResolutionY);
            int classx = (int)(0.2719 * ResolutionX);
            induceclick(classx, classy);
            return;
        }

        //rebuild class is is y 264, slot is y 282
        public static void startrebuildclass()
        {
            int classy = (int)(0.44 * ResolutionY);
            int classx = (int)(0.2719 * ResolutionX);
            induceclick(classx, classy);
            return;
        }

        public static void startrebuildslot()
        {
            int classy = (int)(0.47 * ResolutionY);
            int classx = (int)(0.2719 * ResolutionX);
            induceclick(classx, classy);
            return;
        }

        public static void startfabclassweps()
        {
            //344 137 is the second crafting menu
            //fabricate class weapons should be at 358 175
            int classy = (int)(0.2283 * ResolutionY);
            int classx = (int)(0.26875 * ResolutionX);
            induceclick(classx, classy);
            classy = (int)(0.2917 * ResolutionY);
            classx = (int)(0.28 * ResolutionX);
            induceclick(classx, classy);
            return;
        }

        
        public static Color GetColorAt(Point location)
        {
            //necessary for busy wait on craft button.
            Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        public static void clickokoncraft(int x, int y)
        {
            //there's a busy wait component to this one
            Point craftpixel = new Point(x, y);
            Color btncolor;
            int i = 0;//note that using an indivisible amount of weapons may cause some hanging... you can obviously manually resolve it, but i think i'll try instead to cut it off at 5s
            bool craftdone = false;
            while (!craftdone&&i<100)
            {
                Thread.Sleep(125);
                i++;
                btncolor=GetColorAt(craftpixel);
                if (btncolor.R > 100)
                { craftdone = true; }

            }

            induceclick(x, y);

        }

        public static void induceclick(int x, int y)
        {

            //Console.WriteLine("click at x: " + x + " y: "+y);
            
            SetCursorPos(x,y);
            Thread.Sleep(delay);//need to figure out minimum time for cursor input to be registered
                              //note:  100 looks somewhat good, but it sometimes desyncs, resulted in me mulching my decal'd objector
                              //this is dependent on your computer of course.  i can freely run this while my comp is charging, but when not, it will desync due to worse performance
                              //while you're sussing out your ideal delay, i would recommend 
                              //A) do not do this while anything you care about is in the same row and column, but a prior page as the thing getting mulched
                              //B) do not do this while anything you care about is in r0c1 or r0c2, as these correspond to the crafting menu spots
                              //the biggest candidates for desyncs are paging, pressing continue after a successful craft, and entering the "select items" menu from the crafting menu.
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0,0, 0,new UIntPtr(0));
            
            return;
        }
        
    }
}
