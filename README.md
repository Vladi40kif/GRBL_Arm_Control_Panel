# GRBL Arm control panel

Simple and chip program for control robotic arm.   

[VIDEO](https://youtu.be/OGnDuEYZ1Iw)

![alt text](https://github.com/Vladi40kif/GRBL_Arm_Control_Panel/blob/master/img1.jpg?raw=true)

## NO need installation

Just run GRBL_Arm_Control_Panel/WpfApp2/bin/Debug/netcoreapp3.1/WpfApp1.exe


## Instructions

First of all u need connect arm too computer. 
After choose your COM and badrete.
When u successfully connect to board, u can use any console comand what u want.

For test connection use command:
```bash
$$
```
Board send request with ver. and other info about GRBL...

### Keyboard control

X Axis:
	F1 - increment
	F2 - decrement

Z Axis:
	F3 - increment
	F4 - decrement       

Y Axis:
	F5 - increment
	F6 - decrement        

Spindel PWM:
	PageUp   - increment 
	PageDown - decrement  

Set zero by all axis: 
	Home - set 
	
### Joystic control

Joystic control not need any configuration. Program use DirectX.

## Mechanics part

My test arm asembling from uStepper [robot arm](https://www.thingiverse.com/thing:986224)
Parts print in 3d-printer.

## Software part

My test arm using [GRBL software](https://github.com/grbl/grbl).

## Electronic part

Electronic asembling with: Arduino UNO, CNC Shield and A4988 driver.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)