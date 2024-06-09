import pandas as pd
import utils
import os
import numpy as np
import glob


def squeeze_nan(x):
    original_columns = x.index.tolist()

    squeezed = x.dropna()
    squeezed.index = [original_columns[n] for n in range(squeezed.count())]

    return squeezed.reindex(original_columns, fill_value=np.nan)

outputFilePath = "."
dataFilePath = "./preprocessedData"

file_names = glob.glob(dataFilePath + "/*.csv")

totalData = np.empty((0,2))
custom_sort = ['0', '1']
for file_name in file_names:
    temp = np.array(pd.read_csv(file_name, sep=",", encoding='utf-8'))
    totalData = np.append(totalData, temp, axis=0)

total = pd.DataFrame(totalData)
total.to_csv(outputFilePath + "/total.csv", index=None, header=None)

