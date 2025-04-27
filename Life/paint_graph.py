import matplotlib.pyplot as plt
import os

x = []
y = []
with open('Life/data.txt', 'r') as file:
    for line in file:
        values = line.strip().split()
        x.append(float(values[0].replace(',', '.')))
        y.append(int(values[1]))


fig, ax = plt.subplots()
ax.plot(x, y)
plt.title("График перехода в стабильное состояние")
plt.xlabel('Плотность распределения')
plt.ylabel('Поколение')
plt.grid(True)
plt.show()