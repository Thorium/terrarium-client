//===============================================================================
//
// Class:            Janis
//
// Description:      A herbivore animal for Microsoft Terrarium. Eat plants.
//
// History:          2002-11-30 Tuomas Hietanen - First version
//                   2002-12-07 Tuomas Hietanen - Janis released to Terrarium
//                   2002-12-12 Tuomas Hietanen - Minor update
//
//===============================================================================

using System; 
using System.Drawing; 
using System.Collections; 
using System.IO;

[assembly: OrganismClass("Tuomas_Hietanen.Terrarium.Janistelyt.Janis2")]
[assembly: AuthorInformation("Tuomas Hietanen", "tuomas.hietanen@tietoteollisuus.com")]

namespace Tuomas_Hietanen.Terrarium.Janistelyt {

 #region Elukan määrittelyt  
   [CarnivoreAttribute(false)]
   [AnimalSkin(AnimalSkinFamilyEnum.Inchworm)]
   [MarkingColor(KnownColor.BurlyWood)]
   // Päädyin seuraaviin arvoihin pitkän testailun jälkeen:
   [MatureSize(25)]
   [MaximumEnergyPoints(24)]
   [EatingSpeedPoints(2)]
   [AttackDamagePoints(12)]
   [DefendDamagePoints(28)]
   [MaximumSpeedPoints(24)]
   [CamouflagePoints(0)]
   [EyesightPoints(10)]

 #endregion

   #region Luokkamuuttujat 
   
      // Tää koodi on ihan mua itteäni varten, joten
      // ei ole niin väliks jos on monta luokkamuuttujaa...

      private PlantState kohdeKukka = null;
      private AnimalState hyokkaajaElukka = null;
      int i = new System.Int32();
      private Point vanhemmanSijainti;
      private int kiertoYritys=0;
      private int olenTappaja;               
      private int olenLisaantyja; 
      private int olenMaagi;
      private int olenVanhempi=0;
      char c;
      private int xx;
      private int yy;

      private int TILAONRUOKAA=1; 
      private int TILAHYVAOLO=2; 
      private int TILABLOKATTU=3; 
      private int TILAPAKENEN=5; 

      private int Aktiivisuus = 1;

      private int Pakomatka = 160;
      private int ReittiX;
      private int ReittiY;
      private ArrayList Biomatsku;
      private double Alkusuunta;
      private Point alkuperaKohde;
      private int KasviBlokki=0;

   #endregion
   
   #region Pakollisuudet 

      /// <summary>
      /// Eventtien käsittely
      /// </summary>
      protected override void Initialize() { 
         Load += new LoadEventHandler(LoadEvent); 
         Idle += new IdleEventHandler(IdleEvent); 
         Attacked += new AttackedEventHandler(AttackedEvent); 
         Born += new BornEventHandler(Synnyin);
         MoveCompleted += new MoveCompletedEventHandler(MoveCompletedEvent);
         Teleported += new TeleportedEventHandler(TeleportedEvent);
         AttackCompleted += new AttackCompletedEventHandler(AttackCompletedEvent);
         //EatCompleted += new EatCompletedEventHandler(EatCompletedEvent);
         ReproduceCompleted += new ReproduceCompletedEventHandler(SynnytysOhi);
         DefendCompleted += new DefendCompletedEventHandler(DefendCompletedEvent);
         alkuperaKohde.X=0;
      } 
  
      /// <summary>
      /// Eka juttu joka vuoro
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void LoadEvent(object sender, LoadEventArgs e) { 
         try { 
            setYstavyys();
            if(!(State.ActualDirection==0)){
               Alkusuunta=Vector.ToRadians(State.ActualDirection-180);
            }
            setATila(TILAHYVAOLO);
            Biomatsku = Scan();
            if(!CanEat)setATila(TILAONRUOKAA);
            if(kohdeKukka != null) { 
               kohdeKukka = (PlantState) LookFor(kohdeKukka); 
               Random rnd1 = new Random(unchecked((int)DateTime.Now.Ticks));
               if(kohdeKukka == null){
                  int puhu = Convert.ToInt32(rnd1.NextDouble()*2);
                  if(puhu==1)WriteTrace("Kohde katos."); 
                  if(puhu==2)WriteTrace("Hukkasin kohteen.");
               }
            } 
         } 
         catch(Exception e2) { 
            WriteTrace(e2.ToString()); 
         } 
      } 


      /// <summary>
      /// Kun muut on jo menny
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void IdleEvent(object sender, IdleEventArgs e) { 
         Random rnd1 = new Random(unchecked((int)DateTime.Now.Ticks));
         try { 
            // Reproduce as often as possible 
            if(CanReproduce)Lisaanny();
            // Scan for potential attackers
            if(KasviBlokki==0){
               if (TsekkaaElukat())return;
            } else {
               int puhu = Convert.ToInt32(rnd1.NextDouble()*4);
               if(puhu==1)WriteTrace("Pois tielta kukka!!!");
               if(puhu==2)WriteTrace("Mista tahan tielle tallanen este tupsahti?");
               if(puhu==3)WriteTrace("Pois alta risut ja mannynkavyt");
               return;
            }
            // If we can eat and we have a target plant, eat 
            if(CanEat) { 
               if(!IsEating) { 
                  if(kohdeKukka != null) { 
                     if (kohdeKukka.PercentInjured < 0.75 || State.EnergyState == EnergyState.Deterioration) {
                        if(WithinEatingRange(kohdeKukka)) { 
                           int puhu = Convert.ToInt32(rnd1.NextDouble()*3);
                           if(puhu==1)WriteTrace("Hiphei, loysin ruokaa.");
                           if(puhu==2)WriteTrace("Kukka loyty, ei ku syomaan."); 
                           BeginEating(kohdeKukka); 
                           if(IsMoving)StopMoving();
                        } 
                        else { 
                           if(!IsMoving) { 
                              int puhu = Convert.ToInt32(rnd1.NextDouble()*4);
                              if(puhu==1)WriteTrace("Kukka loyty, joten mennaan sen luokse"); 
                              if(puhu==2)WriteTrace("Kipin kapin kukalle."); 
                              if(puhu==3)WriteTrace("Matelua...kohteena kasvis"); 
                              int nopeus;
                              nopeus=SopivaNopeus();
                              while(nopeus > 2 && State.EnergyRequiredToMove(DistanceTo(kohdeKukka), nopeus) > State.StoredEnergy) {
                                 nopeus /= 2;
                              }
                              if(nopeus<=2){
                                 nopeus = 2;
                              }
                              
                              BeginMoving(new MovementVector(kohdeKukka.Position, nopeus)); 
                           } 
                        } 
                     }
                  } else { 
                     if(!TsekkaaKukkaset()){
                        if(!IsMoving) { 
                           WriteTrace("Tarttis kukkaa..."); 
                           
                           BeginMoving(uusiHauskaKohde());
                        } 
                        else { 
                           int puhu = Convert.ToInt32(rnd1.NextDouble()*50);
                           if(puhu==1)WriteTrace("Liikutaan ja kattellaan..."); 
                           if(puhu==2)WriteTrace("Vallotan maailman! Hmm...Hmm...Hmm..."); 
                           if(puhu==3)WriteTrace("Tapan kaikki muiden vaivalla koodaamat otokat."); 
                           if(puhu==4)WriteTrace("Tuomas koodaa eniten bugeja. (voih!)");
                           if(puhu==5)WriteTrace("Tekoalya loytyy kuin pienesta kirpusta."); 
                           if(puhu==6)WriteTrace("Kayttelen evoluutioalgoritmeja ja binaaripuita."); 
                           if(puhu==7)WriteTrace("En osaa ajatella. Ainakin luulen niin..."); 
                        } 
                     }
                  } 
               } else { 
                  int puhu = Convert.ToInt32(rnd1.NextDouble()*7);
                  if(puhu==1)WriteTrace("Syopottelen..."); 
                  if(puhu==2)WriteTrace("ROUSK ROUSK"); 
                  if(puhu==3)WriteTrace("...mumps mumps..."); 
                  if(puhu==4)WriteTrace("Ompas kasvikset tanaan tuoreita."); 
                  if(puhu==5)WriteTrace("Tama salaatti kaipaa kastiketta."); 
                  if(IsMoving)StopMoving(); 
               } 
            } else { 
               int puhu = Convert.ToInt32(rnd1.NextDouble()*3);
               if(puhu==1)WriteTrace("Olen kyllainen.");
               if(puhu==2)WriteTrace("...ROYH!");
               setATila(TILAONRUOKAA);
               if(Biomatsku.Count > 0) {
                  foreach(OrganismState organismState in Biomatsku) {
                     if(organismState is AnimalState) {
                        if (IsMySpecies(organismState)){
                           Point newPosition = Position;
                           if(rnd1.NextDouble()>0.5){
                              newPosition.X = Position.X + (Convert.ToInt32(rnd1.NextDouble()*4)-2)*20;
                           } else {
                              newPosition.Y = Position.Y + (Convert.ToInt32(rnd1.NextDouble()*4)-2)*20;
                           }
                           BeginMoving(new MovementVector(newPosition, SopivaNopeus()));
                        }
                     }
                  }
               }
            } 
         } 
         catch(Exception exc) { 
            WriteTrace(exc.ToString()); 
         } 
      } 

   #endregion

   #region Liikkuminen ...
      /// <summary>
      /// Piste. Tällä peitetään olion suora kääntyminen
      /// ja pyöristetään se ympyräksi. Hyödyllinen, ettei
      /// lähdetä takaisin viholliselta karattua tai kadottua.
      /// </summary>
      private Point seuraava {
         get {
            double ero=0;
            alkuperaKohde.X=0;
            Vector vektori = Vector.Subtract(Position, new Point(ReittiX, ReittiY));
            if(vektori.Magnitude<50)return new Point(ReittiX, ReittiY);
            if(Alkusuunta==0){
               if(IsMoving)Alkusuunta=vektori.Direction;
            }
            ero = (vektori.Direction - Alkusuunta);

            /*            while((ero>0.125)&&(i<32)){
                           ero = Math.Abs(vektori.Direction - Alkusuunta);
                           if(ero<Math.PI*1.0375)vektori=vektori.Rotate(0.075*Math.PI);
                           if(ero>=Math.PI*1.0375)vektori=vektori.Rotate(-0.075*Math.PI);
                           i++;
                        }*/
            if((ero<-Math.PI)||(ero>Math.PI)){
               if(ero>Math.PI){
                  vektori=vektori.Rotate((Math.PI*2-ero)*0.8);
               } else {
                  vektori=vektori.Rotate(-(Math.PI*2-ero)*0.8);
               }
            } else {
               vektori=vektori.Rotate((-ero)*0.8);
            }

            Vector vektori3 = vektori.GetUnitVector().Scale(40);
            
            Point p = Vector.Add(Position, vektori3);

            if((p.X>1)&&(p.X<WorldWidth-2)&&(p.Y>1)&&(p.Y<WorldHeight-2)){
               return p;
            } else {
               return new Point(ReittiX, ReittiY);
            }
         }
      }


      /// <summary>
      /// Pakonopeus
      /// </summary>
      /// <returns></returns>
      private int KarkuNopeus() {
         int possibleSpeed = Species.MaximumSpeed;
         if(State.EnergyState==EnergyState.Deterioration)possibleSpeed=Convert.ToInt32(possibleSpeed*0.6);
         return possibleSpeed;
      }

      /// <summary>
      /// Arvotaan hyvä x-koordinaatti yleisen liikunnan kohteeksi.
      /// </summary>
      /// <returns>Koordinaatti, jossa ei ole lihansyöjiä... ;)</returns>
      private int ArvoX(){
         int i = OrganismRandom.Next(0+(WorldWidth-1)/20, WorldWidth-1-(WorldWidth-1)/20);
         while((i>(WorldWidth-1)*0.2)&&(i<(WorldWidth-1)*0.8)){
            i = OrganismRandom.Next(0+(WorldWidth-1)/20, WorldWidth-1-(WorldWidth-1)/20);
         }
         return i;
      }   

      /// <summary>
      /// Arvotaan hyvä y-koordinaatti yleisen liikunnan kohteeksi.
      /// </summary>
      /// <returns>Koordinaatti, jossa ei ole lihansyöjiä... ;)</returns>
      private int ArvoY(){
         int i = OrganismRandom.Next(0+(WorldHeight-1)/20, WorldHeight-1-(WorldHeight-1)/20);
         while((i>(WorldHeight-1)*0.2)&&(i<(WorldHeight-1)*0.8)){
            i = OrganismRandom.Next(0+(WorldHeight-1)/20, WorldHeight-1-(WorldHeight-1)/20);
         }
         return i;
      }   


      /// <summary>
      /// Liikunta loppu ny
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MoveCompletedEvent(object sender, MoveCompletedEventArgs e) {
         try{
            Random rnd1 = new Random(unchecked((int)DateTime.Now.Ticks));
            if(e.Reason == ReasonForStop.Blocked) {
               if(IsMoving)StopMoving();
               OrganismState organismState = e.BlockingOrganism;
               int puhu = Convert.ToInt32(rnd1.NextDouble()*4);
               if(puhu==1)WriteTrace("Mut on blokattu!!!");
               if(puhu==2)WriteTrace("Nyt iski ahtaan paikan kammo!");
               if(puhu==3)WriteTrace("Oikeassa luonnossa ei olisi ahdasta!");
               alkuperaKohde = e.MoveToAction.MovementVector.Destination;
               Vector originalVector = Vector.Subtract(alkuperaKohde, this.Position);
               if(hyokkaajaElukka!=null){
                  puhu = Convert.ToInt32(rnd1.NextDouble()*3);
                  setATila(TILAPAKENEN);
                  if(puhu==1)WriteTrace("Se tulee paalle!");
                  if(puhu==2)WriteTrace("Mulla on tunne etta mua seurataan!");
                  if(puhu==3)WriteTrace("Apua! Suihkuta akkia ohvia ja raidia mun vihulaiseen!");
                  //Pitaisi olla: Tai mittarimato!
                  int taiKasvisSyoja=0;
                  if(organismState is AnimalState){
                     IAnimalSpecies iaStat = (IAnimalSpecies)organismState;
                     if(!IsMySpecies(organismState)&&(!iaStat.IsCarnivore)){
                        taiKasvisSyoja=1;
                     }
                  }
                  if((organismState is PlantState)||(!organismState.IsAlive)||(taiKasvisSyoja==1)){
                     double Suunta = originalVector.Direction;
                     Point Desti=Position;
                     if ((Suunta>=Math.PI*0.25)&&(Suunta<Math.PI*0.75)){
                        if(hyokkaajaElukka.Position.X>Position.X){
                           Desti.X=Desti.X-38;
                        } else {
                           Desti.X=Desti.X+38;
                        }
                     }
                     if ((Suunta>=Math.PI*1.25)&&(Suunta<Math.PI*1.75)){
                        if(hyokkaajaElukka.Position.X>Position.X){
                           Desti.X=Desti.X-38;
                        } else {
                           Desti.X=Desti.X+38;
                        }
                     }
                     if ((Suunta>=Math.PI*0.75)&&(Suunta<Math.PI*1.25)){
                        if(hyokkaajaElukka.Position.Y>Position.Y){
                           Desti.Y=Desti.Y-38;
                        } else {
                           Desti.Y=Desti.Y+38;
                        }
                     }
                     if ((Suunta>=Math.PI*1.75)||(Suunta<Math.PI*0.25)){
                        if(hyokkaajaElukka.Position.Y>Position.Y){
                           Desti.Y=Desti.Y-38;
                        } else {
                           Desti.Y=Desti.Y+38;
                        }
                     }
                     KasviBlokki=1;
                     BeginMoving(new MovementVector(Desti, KarkuNopeus()));
                     return;
                  }
               } else {
                  if(kohdeKukka!=null){
                     if(CanEat){
                        if(!IsEating){
                           try{
                              BeginEating(kohdeKukka);
                              return;
                           } catch {
                              setATila(TILABLOKATTU);
                           }
                        }
                     }
                  }
               }
               if((kohdeKukka!=null)||(hyokkaajaElukka!=null)||(originalVector.Magnitude<100)){

                  double Suunta = originalVector.Direction;
                  Point Desti=Position;
                  if ((Suunta>=Math.PI*0.25)&&(Suunta<Math.PI*0.75)){
                     if(hyokkaajaElukka!=null){
                        if(hyokkaajaElukka.Position.X>Position.X){
                           Desti.X=Desti.X-38;
                        } else {
                           Desti.X=Desti.X+38;
                        }
                     } else {
                        if(rnd1.NextDouble()>0.5){
                           Desti.X=Desti.X-38;
                        } else {
                           Desti.X=Desti.X+38;
                        }
                     }
                  }
                  if ((Suunta>=Math.PI*1.25)&&(Suunta<Math.PI*1.75)){
                     if(hyokkaajaElukka!=null){
                        if(hyokkaajaElukka.Position.X>Position.X){
                           Desti.X=Desti.X-38;
                        } else {
                           Desti.X=Desti.X+38;
                        }
                     } else {
                        if(rnd1.NextDouble()>0.5){
                           Desti.X=Desti.X-38;
                        } else {
                           Desti.X=Desti.X+38;
                        }
                     }
                  }

                  if ((Suunta>=Math.PI*0.75)&&(Suunta<Math.PI*1.25)){
                     if(hyokkaajaElukka!=null){
                        if(hyokkaajaElukka.Position.Y>Position.Y){
                           Desti.Y=Desti.Y-38;
                        } else {
                           Desti.Y=Desti.Y+38;
                        }
                     } else {
                        if(rnd1.NextDouble()>0.5){
                           Desti.Y=Desti.Y-38;
                        } else {
                           Desti.Y=Desti.Y+38;
                        }
                     }
                  }
                  if ((Suunta>=Math.PI*1.75)||(Suunta<Math.PI*0.25)){
                     if(hyokkaajaElukka!=null){
                        if(hyokkaajaElukka.Position.Y>Position.Y){
                           Desti.Y=Desti.Y-38;
                        } else {
                           Desti.Y=Desti.Y+38;
                        }
                     } else {
                        if(rnd1.NextDouble()>0.5){
                           Desti.Y=Desti.Y-38;
                        } else {
                           Desti.Y=Desti.Y+38;
                        }
                     }
                  }
               
                  if(kiertoYritys>100){
                     WriteTrace("Etsitaan uusi kohde");
                     kiertoYritys=0;
                     BeginMoving(uusiHauskaKohde());
                  }
                  if (hyokkaajaElukka != null){
                     BeginMoving(new MovementVector(Desti, KarkuNopeus()));
                  } else {
                     BeginMoving(new MovementVector(Desti, SopivaNopeus()));
                  }

               }
            } else {
               KasviBlokki=0;
               int puhu = Convert.ToInt32(rnd1.NextDouble()*4);
               if(puhu==1)WriteTrace("Yritan viela alkuperaispaikkaan...");
               if(puhu==2)WriteTrace("Tuolla esteen takana siintaa hauska kohde.");
               if(puhu==3)WriteTrace("Koetan vielakin kiertaa.");
               if(alkuperaKohde.X!=0){
                  if (hyokkaajaElukka != null){
                     BeginMoving(new MovementVector(alkuperaKohde, KarkuNopeus()));
                  } else {
                     BeginMoving(new MovementVector(alkuperaKohde, SopivaNopeus()));
                  }
                  alkuperaKohde.X=0;
               }
            }
         } catch(Exception e2) {
            WriteTrace(e2);
         }
      }


      /// <summary>
      /// Vaelletaan ympäriinsä
      /// </summary>
      private MovementVector uusiHauskaKohde() {

         if ((Math.Abs(ReittiX-Position.X)<100)&&(Math.Abs(ReittiY-Position.Y)<100)){
         
            Random rnd1 = new Random(unchecked((int)DateTime.Now.Ticks));
            int i = Convert.ToInt32(rnd1.NextDouble()*20);
         
            if(State.Generation>0){
               ReittiX = vanhemmanSijainti.X;
               ReittiY = vanhemmanSijainti.Y;
            } else {
               ReittiX = xx; ReittiY = yy;
            }
         
            if((i==1)||(i==2)){
               ReittiX=OrganismRandom.Next(0, WorldWidth - 1);
               ReittiY=OrganismRandom.Next(0, WorldHeight - 1);
            }
            if((i==3)||(i==4)){
               ReittiX = xx; ReittiY = yy;
            }
            if(i==6){
               ReittiX = Convert.ToInt32((WorldWidth - 1)/2);
               ReittiY = Convert.ToInt32((WorldHeight - 1)/2);
            }
         }

         MovementVector destination = new MovementVector(seuraava, SopivaNopeus());

         return destination;
      }


      /// <summary>
      /// Ja nopeus kuten Mikrosoftin mallissa, mutta otetaan mukaan evoluutio!
      /// </summary>
      /// <returns></returns>
      private int SopivaNopeus() {
         int speed = 0;
         switch(State.EnergyState) {
            case EnergyState.Full:
               speed = Convert.ToInt32(Species.MaximumSpeed * (0.40+Aktiivisuus/10)); 
               break;
            case EnergyState.Normal:
               speed = Convert.ToInt32(Species.MaximumSpeed * (0.31+Aktiivisuus/10)); 
               break;
            case EnergyState.Hungry:
               speed = Convert.ToInt32(Species.MaximumSpeed * (0.16+Aktiivisuus/10));
               break;
            default:
               speed = Convert.ToInt32(Species.MaximumSpeed * (0.06+Aktiivisuus/10));
               break;
         }
         if(speed>Species.MaximumSpeed)speed=Species.MaximumSpeed;
         if(speed<2)speed=2;
         return speed;
      }

   #endregion

   #region DNA & Perinnollisyys & Lisaantyminen 
      /// <summary>
      /// Varmistetaan uudelle lapselle elämän päämäärä, eli
      /// päästä vanhemman paikalle, mikäli se on hyvä!
      /// Ja sitten elukan perusnopeus tulee evoluutiosta!
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Synnyin(object sender, BornEventArgs e) {
         byte[] dna = e.Dna;
         Random rnd1 = new Random(unchecked((int)DateTime.Now.Ticks));
         xx=ArvoX();
         yy=ArvoY();
         setYstavyys();
         setATila(TILAHYVAOLO);

         if ( dna != null ) {
            MemoryStream m = new MemoryStream(dna);
            BinaryReader b = new BinaryReader(m);
            vanhemmanSijainti = new Point(b.ReadInt32(),b.ReadInt32());
         
            switch(Convert.ToInt32(rnd1.NextDouble()*10)) {
               case 1:
                  WriteTrace("Olen erilainen nuori!");
                  Aktiivisuus = b.ReadInt32();
                  Aktiivisuus=Convert.ToInt32(rnd1.NextDouble()*5);
                  if(Aktiivisuus==5)Aktiivisuus=1;
                  break;
               case 2:
                  WriteTrace("Olen vanhempiani nopeampi...");
                  Aktiivisuus = b.ReadInt32()+1;
                  if(Aktiivisuus>4)Aktiivisuus=4;
                  break;
               case 4:
                  WriteTrace("Olen ruma ankanpoikanen...");
                  Aktiivisuus = b.ReadInt32()-1;
                  if(Aktiivisuus<0)Aktiivisuus=0;
                  break;
               default:
                  WriteTrace("Seuraan vanhempieni jalanjalkia.");
                  Aktiivisuus = b.ReadInt32();
                  break;
            }
            switch(Convert.ToInt32(rnd1.NextDouble()*10)) {
               case 1:
                  Pakomatka = b.ReadInt32();
                  Pakomatka=Convert.ToInt32(rnd1.NextDouble()*180)+80;
                  break;
               case 2:
                  Pakomatka = b.ReadInt32()+20;
                  if(Pakomatka>260)Pakomatka=260;
                  break;
               case 4:
                  Pakomatka = b.ReadInt32()-20;
                  if(Pakomatka<80)Pakomatka=80;
                  break;
               default:
                  WriteTrace("Seuraan vanhempieni jalanjalkia.");
                  Pakomatka = b.ReadInt32();
                  break;
            }
            b.Close();
         } else {
            vanhemmanSijainti = new Point(ArvoX(),ArvoY());
         }
         ReittiX=vanhemmanSijainti.X;
         ReittiY=vanhemmanSijainti.Y;
         
         BeginMoving(new MovementVector(seuraava, SopivaNopeus()));
         WriteTrace("Synnytys onnistui! Kuka on onnellinen aiti?");

      }

      
      /// <summary>
      /// Lisääntymisessä perinnöllistetään hyvä sijainti kartalla
      /// ja muutama muu ominaisuus.
      /// </summary>
      private void Lisaanny(){
         MemoryStream m = new MemoryStream();
         BinaryWriter b = new BinaryWriter(m);
         if((State.PercentInjured<0.5)||(State.EnergyState==EnergyState.Deterioration)){
            WriteTrace("Periytan myos olinpaikkani...");
            b.Write(Position.X);
            b.Write(Position.Y);
         } else { 
            WriteTrace("Taalla on liikaa petoja, periytan lapselle mummin olinpaikan..");
            b.Write(vanhemmanSijainti.X);
            b.Write(vanhemmanSijainti.Y);
         }
         b.Write(Aktiivisuus);
         b.Write(Pakomatka);
         BeginReproduction(m.ToArray());
         b.Close();
         WriteTrace("Lisaannyn kuin hiiri!");
      }


      /// <summary>
      /// Synnytys ohi. Nyt voi tappaa muita kasvissyöjiä ilman
      /// populaation vaarantumista!
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      public void SynnytysOhi(object sender, ReproduceCompletedEventArgs e) {
         olenLisaantyja += 1;
         olenVanhempi=1;
      }


      /// <summary>
      /// Serialisointi
      /// </summary>
      /// <param name="m"></param>
      public override void SerializeAnimal(MemoryStream m) { 
         try{
            BinaryWriter b = new BinaryWriter(m);
            b.Write(olenTappaja);
            b.Write(olenLisaantyja);
            b.Write(olenMaagi);
            b.Write(Pakomatka);
            b.Write(Aktiivisuus);
         }
         catch {
            WriteTrace("Muisti meni, mutta ei haittaa!");
         }
      } 
  
      /// <summary>
      /// Deserialisointi...
      /// </summary>
      /// <param name="m"></param>
      public override void DeserializeAnimal(MemoryStream m) { 
         try{
            BinaryReader b = new BinaryReader(m);
            olenTappaja = b.ReadInt32();
            olenLisaantyja = b.ReadInt32();
            olenMaagi = b.ReadInt32();
            Pakomatka = b.ReadInt32();
            Aktiivisuus = b.ReadInt32();
         }
         catch {
            WriteTrace("En saanut muistitietoja...");
         }
      } 

   #endregion

   #region Hyokkays & Puolustus 
      /// <summary>
      /// Joku hyokkaa paalle
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void AttackedEvent(object sender, AttackedEventArgs e) { 
         try {
            if(e.Attacker.IsAlive) { 
               int nopeus;

               AnimalState hyokkaaja = e.Attacker;
               bool Yst = false;
               try{
                  Yst = (OnYstava(hyokkaaja.Antennas.AntennaValue, hyokkaaja.Generation));
               } catch {
                  Yst = false;
               }
               if(!Yst){ 
                  nopeus=hyokkaaja.AnimalSpecies.MaximumSpeed+1;
                  if(nopeus>Species.MaximumSpeed)nopeus=Species.MaximumSpeed;
                  BeginDefending(hyokkaaja);
                  if(((State.PercentInjured>48)&&(hyokkaaja.PercentInjured<=State.PercentInjured))||(olenVanhempi==0)||(State.Generation<3)){
                     WriteTrace("Turpaan tulee. Yritan karkuun.");
                     Vector newPositionVector;
                     Vector newVector = Vector.Subtract(hyokkaaja.Position, Position);
                     newPositionVector = newVector.Scale(1.6);
                     Point newPosition = Vector.Add(Position, newPositionVector);
                     setATila(TILAPAKENEN);
                     BeginMoving(new MovementVector(newPosition, KarkuNopeus()));
                     hyokkaajaElukka = hyokkaaja;
                  }
                  if(CanAttack(hyokkaaja))BeginAttacking(hyokkaaja);
               }
            } 
         }
         catch(Exception e2){
            WriteTrace("Hyokkaaja aiheutti henkisia vaurioita. "+e2.ToString());
         }
      } 

      
      /// <summary>
      /// Puolustus valmis...mutta ei oikeen oo sopivaa tekemista.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      public void DefendCompletedEvent(object sender, DefendCompletedEventArgs e) {
      }

      /// <summary>
      /// Hyokkays ohi.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      public void AttackCompletedEvent(object sender, AttackCompletedEventArgs e) {
         if ( e.Killed ) { 
            olenTappaja += 1; 
            WriteTrace("Tapoin sen ???");
            if((State.EnergyState==EnergyState.Normal)||(State.EnergyState==EnergyState.Full)){
               try{
                  State.HealDamage();
               }
               catch {
                  WriteTrace("Huh huijaa.");
               }
            }
         }
      }

   #endregion
    
   #region Skannaus ja kommunikaatio 

      /// <summary>
      /// Antennin vaantoa. Kerrotaan olotila antennissa lajitovereille.
      /// </summary>
      /// <param name="Tila"></param>
      private void setATila(int Tila){
         try {
            string _temp;
            _temp=Antennas.AntennaValue.ToString();
            if(_temp.Length>1){
               Antennas.AntennaValue=Convert.ToInt32(_temp.Substring(0,1)+Tila.ToString());
            } else {
               Antennas.AntennaValue=Tila;
            }
         } catch {
            WriteTrace("Tilansaato epaonnistui.");
         }
      }


      /// <summary>
      /// Ystavallinen antenninkoodi...rotujen valista tiedonsiirtoa.
      /// </summary>
      private void setYstavyys(){
         try{
            DateTime t = new DateTime();
            t = DateTime.Now;
            int chk=((t.Minute+State.Generation+3)%7+1);
            if(Antennas.AntennaValue>9){
               Antennas.AntennaValue=Convert.ToInt32(chk.ToString()+ "" + Antennas.AntennaValue.ToString().Substring(1,1));
            } else {
               Antennas.AntennaValue=chk*10;
            }
         } catch {
            Antennas.AntennaValue=0;
            WriteTrace("Olen sinkku ja kuolen pian");
         }
      }


      /// <summary>
      /// Onko toisen rodun edustaja ystava?
      /// </summary>
      /// <param name="aValue"></param>
      /// <param name="generaatio"></param>
      /// <returns></returns>
      private bool OnYstava(int aValue, int generaatio){
         try {
            string _temp;
            DateTime t = new DateTime();
            t = DateTime.Now;

            _temp=aValue.ToString();

            if(_temp.Length>1){
               if((Convert.ToInt32(_temp.Substring(0,1))==(Convert.ToInt32((t.Minute+generaatio+3)%7+1)))){
                  return true;
               }
            }
            return false;
         } catch {
            WriteTrace("En tieda onko han ystava...");
            return false;
         }
      }


      /// <summary>
      /// Kommunikaatio elainten kanssa
      /// </summary>
      /// <returns></returns>
      private bool TsekkaaElukat() {
         try {
            Random rnd1 = new Random(unchecked((int)DateTime.Now.Ticks));
            if(Biomatsku.Count > 0) {
               
               #region Hyokkaaja
               foreach(OrganismState organismState in Biomatsku) {
                  if(organismState is AnimalState) {
                     IAnimalSpecies Elukka = (IAnimalSpecies) organismState.Species;
                     if (Elukka.IsCarnivore) {
                        hyokkaajaElukka = (AnimalState)organismState;
                        bool Yst = false;
                        try{
                           Yst = (OnYstava(hyokkaajaElukka.Antennas.AntennaValue, hyokkaajaElukka.Generation));
                        } catch {
                           Yst = false;
                        }
                        if((!Yst)&&(hyokkaajaElukka.IsAlive)){
                           Vector newVector = Vector.Subtract(organismState.Position, Position);
                           if((newVector.Magnitude<100)||(WithinAttackingRange ((AnimalState)organismState))) {
                              int puhu = Convert.ToInt32(rnd1.NextDouble()*3);
                              if(puhu==0)WriteTrace("Lihis! Nyt mua viedaan!");
                              if(puhu==1)WriteTrace("Apua, peto! Nyt tuli kiire!");
                              if(puhu==2)WriteTrace("Isoaiti, miksi sinulla on noin suuret hampaat?");
                              Vector newPositionVector;
                              newVector = newVector.GetUnitVector();

                              if(State.EnergyState==EnergyState.Hungry){
                                 newPositionVector = newVector.Scale(Pakomatka);
                              } else {
                                 if(State.EnergyState==EnergyState.Deterioration){
                                    newPositionVector = newVector.Scale(Pakomatka*0.75);
                                 } else {
                                    newPositionVector = newVector.Scale(Pakomatka*1.5);
                                 }
                              }

                              
                              BeginMoving(new MovementVector(Vector.Add(Position, newPositionVector), KarkuNopeus()));
                              return true;
                           }
                        } else {
                           hyokkaajaElukka = null;
                           WriteTrace("Minua suojelee lihansyojaystava.");
                        }
                     }
                  }
               }
               #endregion

               #region Oma varoitustilassa  
               foreach(OrganismState organismState in Biomatsku) {
                  if(organismState is AnimalState) {
                     IAnimalSpecies Elukka = (IAnimalSpecies) organismState.Species;
                     bool Yst = false;
                     try{
                        Yst = (OnYstava(hyokkaajaElukka.Antennas.AntennaValue, hyokkaajaElukka.Generation));
                     } catch {
                        Yst = false;
                     }
                     if ((IsMySpecies(organismState))||(Yst)){
                        Vector newVector = Vector.Subtract(organismState.Position, Position);
                        if((newVector.Magnitude<80)&&(State.EnergyState!=EnergyState.Deterioration)) {
                           hyokkaajaElukka = (AnimalState)organismState;
                           try{
                              if(Convert.ToInt32(hyokkaajaElukka.Antennas.AntennaValue.ToString().Substring(1,1))==TILAPAKENEN){
                                 int puhu = Convert.ToInt32(rnd1.NextDouble()*4);
                                 if(puhu==0)WriteTrace("Kamu juoksee petoa pakoon, annan tilaa...");
                                 if(puhu==1)WriteTrace("Apua, peto lahella kaveria! Nyt tuli kiire!");
                                 if(puhu==2)WriteTrace("Yleinen halyytys!");
                                 if(puhu==3)WriteTrace("Kuulkaa kaikki! Taakse poistu!");
                                 if(puhu==4)WriteTrace("Hei kaikki, nyt vahan liikuntaa!");
                                 Vector newPositionVector;
                                 newVector = newVector.GetUnitVector();

                                 if(State.EnergyState==EnergyState.Hungry){
                                    newPositionVector = newVector.Scale(Pakomatka*0.7);
                                 } else {
                                    if(State.EnergyState==EnergyState.Deterioration){
                                       newPositionVector = newVector.Scale(Pakomatka*0.3);
                                    } else {
                                       newPositionVector = newVector.Scale(Pakomatka*0.9);
                                    }
                                 }

                              
                                 BeginMoving(new MovementVector(Vector.Add(Position, newPositionVector), KarkuNopeus()));
                                 return true;
                              } 
                           } catch {
                              WriteTrace("Ystava pettaa...");
                           }
                        }
                     }
                  }
               }

               #endregion

               #region kasvissyoja
               foreach(OrganismState organismState in Biomatsku) {
                  if(organismState is AnimalState) {
                     IAnimalSpecies Elukka = (IAnimalSpecies) organismState.Species;
                     bool Yst = false;
                     try{
                        Yst = (OnYstava(hyokkaajaElukka.Antennas.AntennaValue, hyokkaajaElukka.Generation));
                     } catch {
                        Yst = false;
                     }
                     if (IsMySpecies(organismState)||(Yst)){
                        if(WithinAttackingRange ((AnimalState)organismState)){
                           if(State.IsStopped){
                              AnimalState aKohde = (AnimalState)organismState;
                              if(Convert.ToInt32(aKohde.Antennas.AntennaValue.ToString().Substring(1,1))==TILABLOKATTU){

                              
                                 int puhu = Convert.ToInt32(rnd1.NextDouble()*3);
                                 if(puhu==0)WriteTrace("Kaveri tuuppii etta pitas antaa tilaa.");
                                 if(puhu==1)WriteTrace("Pitas antaa kaverinkin syoda!");
                                 if(puhu==2)WriteTrace("Mikas itikka se siella takana mesoaa? Mun syonti hairiintyy.");
                                 double Suunta = Vector.ToRadians(organismState.ActualDirection-180);
                                 Point Desti=Position;
                                 if ((Suunta>=Math.PI*0.25)&&(Suunta<Math.PI*0.75)){
                                    if(rnd1.NextDouble()>0.5){
                                       Desti.Y=Desti.X+40;
                                    } else {
                                       Desti.Y=Desti.X-40;
                                    }
                                 }
                                 if ((Suunta>=Math.PI*1.25)&&(Suunta<Math.PI*1.75)){
                                    if(rnd1.NextDouble()>0.5){
                                       Desti.Y=Desti.X+40;
                                    } else {
                                       Desti.Y=Desti.X-40;
                                    }
                                 }

                                 if ((Suunta>=Math.PI*0.75)&&(Suunta<Math.PI*1.25)){
                                    if(rnd1.NextDouble()>0.5){
                                       Desti.X=Desti.Y+40;
                                    } else {
                                       Desti.X=Desti.Y-40;
                                    }
                                 }
                                 if ((Suunta>=Math.PI*1.75)||(Suunta<Math.PI*0.25)){
                                    if(rnd1.NextDouble()>0.5){
                                       Desti.X=Desti.Y+40;
                                    } else {
                                       Desti.X=Desti.Y-40;
                                    }
                                 }

                                 BeginMoving(new MovementVector(Desti, SopivaNopeus()));
                              } 
                           }
                        }
                        try{
                           AnimalState aKohde = (AnimalState)organismState;
                           if(Convert.ToInt32(aKohde.Antennas.AntennaValue.ToString().Substring(1,1))==TILAONRUOKAA){
                              if(kohdeKukka == null){
                                 if((State.EnergyState==EnergyState.Deterioration)||(State.EnergyState==EnergyState.Hungry)){
                                 
                                    BeginMoving(new MovementVector(aKohde.Position,SopivaNopeus()));
                                 }
                              }
                           }
                        } catch {
                           WriteTrace("Antakaa ruokoo!");
                        }
                     } else {
                        if (!Elukka.IsCarnivore){
                           AnimalState aKohde = (AnimalState)organismState;
                           try{
                              Yst = (OnYstava(aKohde.Antennas.AntennaValue, aKohde.Generation));
                           } catch {
                              Yst = false;
                           }
                           if(!Yst){

                              if (CanAttack((AnimalState)organismState)){
                                 if((VoinTappaa(aKohde)>0.48)&&(aKohde.IsAlive)){
                                    if((State.PercentInjured<20)&&(aKohde.PercentInjured>=State.PercentInjured)){
                                       if((olenVanhempi>0)&&(State.Generation>2)){
                                          if(WithinAttackingRange ((AnimalState)organismState)){
                                             try{
                                                WriteTrace("Olen agressiivinen! Tapan monkijan!");
                                                BeginDefending(aKohde); 
                                                BeginAttacking(aKohde);
                                                return true;
                                             }
                                             catch {
                                                WriteTrace("Tekisipa mieleni tappaa tuo viereinen elukka...");
                                             }
                                          } else {
                                             Vector newVector = Vector.Subtract(organismState.Position, Position);
                                             if(newVector.Magnitude<50){
                                                if(State.EnergyState==EnergyState.Hungry){
                                                   BeginMoving(new MovementVector(aKohde.Position,KarkuNopeus()));
                                                }
                                             }
                                          }
                                       }
                                    }
                                 }
                              }
                           }
                        }
                     }
                  }
               }

               #endregion
            }
         }
         catch(Exception e) {
            WriteTrace(e.ToString());
         }
         hyokkaajaElukka = null;
         return false;
      }


      /// <summary>
      /// Todennakoisyys tappaa tai tulla tapetuksi
      /// </summary>
      /// <param name="defender"></param>
      /// <returns></returns>
      private double VoinTappaa(AnimalState defender) {
         try{
            double attackerCanLoose = EngineSettings.DamageToKillPerUnitOfRadius*State.Radius-State.Damage;
            double defenderCanLoose = EngineSettings.DamageToKillPerUnitOfRadius*defender.Radius-defender.Damage;
            if(defenderCanLoose<0) defenderCanLoose=0.01;
            if(attackerCanLoose<0) attackerCanLoose=0.01;
            double attackerAttackDefend  = (State.AnimalSpecies.MaximumAttackDamagePerUnitRadius+State.AnimalSpecies.MaximumDefendDamagePerUnitRadius)*State.Radius;
            double defenderAttackDefend  = (defender.AnimalSpecies.MaximumAttackDamagePerUnitRadius+defender.AnimalSpecies.MaximumDefendDamagePerUnitRadius)*defender.Radius;
            double risk = 0.05;
            return risk + ((attackerCanLoose*attackerAttackDefend) / (defenderCanLoose*defenderAttackDefend));
         }
         catch {
            WriteTrace("En osaa laskea...");
            return 0; 
         }
      }


      /// <summary>
      /// Kukkien etsintaa.
      /// </summary>
      /// <returns></returns>
      private bool TsekkaaKukkaset() { 
         try { 
            if(Biomatsku.Count > 0) { 
               // Always move after closest plant or defend closest creature if there is one 
               foreach(OrganismState organismState in Biomatsku) { 
                  if(organismState is PlantState) { 
                     kohdeKukka = (PlantState) organismState;
                     int nopeus;
                     nopeus=SopivaNopeus();
                     while(nopeus > 2 && State.EnergyRequiredToMove(DistanceTo(organismState), nopeus) > State.StoredEnergy) {
                        nopeus /= 2;
                     }
                     if(nopeus<=2){
                        nopeus = 2;
                     }

                     
                     BeginMoving(new MovementVector(organismState.Position, nopeus)); 
                     return true; 
                  } 
               } 
            }
         }
         catch(Exception e) { 
            WriteTrace(e.ToString()); 
         } 
         return false; 
      } 


   #endregion

   #region Muut...  

      /// <summary>
      /// Teleporttaus
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      public void TeleportedEvent(object sender, TeleportedEventArgs e) {
         try {
            olenMaagi += 1;
            kohdeKukka = null;
            hyokkaajaElukka = null;
            ReittiX = Position.X;
            ReittiY = Position.Y;
            
            BeginMoving(uusiHauskaKohde());
         }
         catch {
            WriteTrace("Olen huono unohtamaan asioita...");
         }
      }

   #endregion

   } //class

} //namespace