import torch
import torch.nn as nn
import torch.optim as optim
import math
from torch.utils.data import DataLoader
from torch.utils.data import Dataset
import numpy as np
from abc import *


class BasisModel(nn.Module, metaclass=ABCMeta):

    @abstractmethod
    def forward(self):
        raise NotImplementedError

    @abstractmethod
    def Training(self):
        raise NotImplementedError

    @abstractmethod
    def evaluate(self):
        raise NotImplementedError

class LSTMModel(BasisModel):
    def __init__(self, in_size, hidden_number, out_size, batch_size, epoch,sequence_length, device, loss_function, learning_rate, lstm_layer_number):
        super(BasisModel, self).__init__()
        self.inSize = in_size
        self.n_hidden = hidden_number
        self.outSize = out_size
        self.batchSize = batch_size
        self.epoch = epoch
        self.sequence_length = sequence_length
        self.device = device
        self.lossFunction = loss_function
        self.learningRate = learning_rate
        self.LayerNum = lstm_layer_number
        self.lstm1 = nn.LSTM(input_size = self.inSize, hidden_size=self.n_hidden, num_layers=self.LayerNum, batch_first=True)
        self.dropout = nn.Dropout(p= 0.3) # only for training
        self.dense = nn.Linear(self.n_hidden, self.outSize)
        self.tanh = nn.Tanh()
        #self.Relu = nn.ReLU()
        self.softMax = nn.Softmax(dim=2)
        self.optimizer = optim.Adam(self.parameters(), lr=self.learningRate)

    def forward(self, input):
        self.out, (self.h1, self.c1) = self.lstm1(input)
        self.out = self.tanh(self.out)
        self.out = self.dropout(self.out)
        self.out = self.dense(self.out)

        return self.out

    def Training(self, dataLoader):
        self.train()
        for k in range(self.epoch):
            for i, datas in enumerate(dataLoader.__iter__()):
                # print("datas " + str(datas))
                x = datas[0].to(self.device)
                y = datas[1].to(self.device)

                output = self.forward(x)
                loss = self.lossFunction(output[:, self.sequence_length - 1, :], target=y[:, :])

                self.optimizer.zero_grad()
                loss.backward()
                self.optimizer.step()

            print("Epoch" + str((k + 1)) + "loss : " + str(loss))

    def evaluate(self,dataLoader,mode):
        return self.Pos_evaluate(dataLoader,mode)

    def onehot_evaluate(self, dataLoader, mode):

        self.eval()
        correct = 0
        total = 0

        with torch.no_grad():
            for i, datas in enumerate(dataLoader):
                x = datas[0].to(self.device)
                y = datas[1].to(self.device)
                output = self.forward(x)
                predicted = torch.argmax(output[:, self.sequence_length - 1], dim=1)
                label = torch.argmax(y, 1)
                total += y.size(0)
                correct += (predicted == label).sum().item()

            print("{} Accracy of the model : {} %".format(mode, 100 * correct / total))
        return 100 * correct / total

    def Pos_evaluate(self, dataLoader, mode):
        self.eval()
        count = 0
        targetdistance = 0
        distances = torch.empty(0).to(self.device)
        with torch.no_grad():
            for i, datas in enumerate(dataLoader):
                x = datas[0].to(self.device)
                y = datas[1].to(self.device)
                output = self.forward(x)
                predicted = output[:, self.sequence_length - 1]
                temp = torch.sqrt((predicted[:, 0] - y[:, 0]) ** 2 + (predicted[:, 1] - y[:, 1]) ** 2)
                distances = torch.cat([distances,temp], dim=0 )
                targetdistance += torch.sqrt((y[:,0])**2 + (y[:,1])**2).sum()

                count += y.size(0)
            distances = torch.tensor(distances)
            stdDistance, meanDistance = torch.std_mean(distances)
            meanTargetDistance = targetdistance / count

            print("")
            print("{} Accracy of the model ... mean distance {} // mde : {}  std : {}".format(mode, meanTargetDistance,meanDistance, stdDistance))
        return meanDistance

class GRUModel(LSTMModel):
    def __init__(self, in_size, hidden_number, out_size, batch_size, epoch,sequence_length, device, loss_function, learning_rate, lstm_layer_number):
        super(LSTMModel, self).__init__()
        self.inSize = in_size
        self.n_hidden = hidden_number
        self.outSize = out_size
        self.batchSize = batch_size
        self.epoch = epoch
        self.sequence_length = sequence_length
        self.device = device
        self.lossFunction = loss_function
        self.learningRate = learning_rate
        self.LayerNum = lstm_layer_number
        self.dropout = nn.Dropout(p= 0.3) # only for training
        self.dense = nn.Linear(self.n_hidden, self.outSize)
        self.gru = nn.GRU(input_size= self.inSize, hidden_size=self.n_hidden, num_layers=self.LayerNum, batch_first=True )
        #self.transformer = nn.Transformer()
        self.tanh = nn.Tanh()
        #self.Relu = nn.ReLU()
        self.softMax = nn.Softmax(dim=2)
        self.optimizer = optim.Adam(self.parameters(), lr=self.learningRate)

    def forward(self, input):
        self.out, (self.h1, self.c1) = self.gru(input)
        self.out = self.tanh(self.out)
        self.out = self.dropout(self.out)
        self.out = self.dense(self.out)
        return self.out

class Transformer(BasisModel):
    # reference : https://wikidocs.net/156986
    # Constructor
    def __init__( self, num_tokens, dim_model, num_heads, num_encoder_layers, num_decoder_layers, dim_feedforward,  dropout_p, loss_function,  learning_rate, device):
        super(BasisModel, self).__init__()

        # INFO
        self.model_type = "Transformer"
        self.dim_model = dim_model
        self.num_tokens = num_tokens
        self.loss_fn = loss_function
        self.lr = learning_rate
        self.device = device
        # LAYERS
        self.positional_encoder = PositionalEncoding(dim_model=dim_model, dropout_p=dropout_p, max_len=5000, device=self.device)
        self.embedding = nn.Embedding(num_tokens, dim_model).to(self.device)
        self.transformer = nn.Transformer(
            d_model=dim_model,
            nhead=num_heads,
            num_encoder_layers=num_encoder_layers,
            num_decoder_layers=num_decoder_layers,
            dim_feedforward=dim_feedforward,
            dropout=dropout_p,
        ).to(device)
        self.encoder = nn.Linear(7, dim_model).to(self.device)
        self.encoder_d = nn.Linear(num_tokens, dim_model).to(self.device)
        self.out = nn.Linear(dim_model, num_tokens).to(device)
        self.opt = optim.Adam(self.parameters(),lr=self.lr)

    def forward(self, src, tgt, tgt_mask=None, src_pad_mask=None, tgt_pad_mask=None):
        # Src, Tgt size 는 반드시 (batch_size, src sequence length) 여야 합니다.

        # Embedding + positional encoding - Out size = (batch_size, sequence length, dim_model)

        #src = self.embedding(src) * math.sqrt(self.dim_model)
        #tgt = self.embedding(tgt) * math.sqrt(self.dim_model)
        src = self.encoder(src)
        tgt = self.encoder_d(tgt)
        src = self.positional_encoder(src)
        tgt = self.positional_encoder(tgt)

        src = src.permute(1,0,2)
        tgt = tgt.permute(1,0,2)

        # Transformer blocks - Out size = (sequence length, batch_size, num_tokens)
        transformer_out = self.transformer(src, tgt, tgt_mask=tgt_mask, src_key_padding_mask=src_pad_mask, tgt_key_padding_mask=tgt_pad_mask)
        out = self.out(transformer_out)

        return out

    def Training(self, dataLoader):
        self.train()
        total_loss = 0

        for i, datas in enumerate(dataLoader):
            #X, y = batch[:, 0], batch[:, 1]
            X = datas[0].to(self.device)
            y = datas[1].to(self.device)
            #tmp = torch.zeros((y.size(0), y.size(1), X.size(2) - y.size(2))).to(self.device)
            #tmp = torch.cat([y, tmp], dim=2)
            X, y = torch.tensor(X).to(self.device, dtype=torch.float32), torch.tensor(y).to(self.device, dtype=torch.float32)

            # 이제 tgt를 1만큼 이동하여 <SOS>를 사용하여 pos 1에서 토큰을 예측합니다.
            y_input = y[:, :-1]
            y_expected = y[:, 1:]

            # 다음 단어를 마스킹하려면 마스크 가져오기
            #sequence_length = y_expected.size(1)
            #tgt_mask = self.get_tgt_mask(sequence_length).to(self.device)
            #tgt_mask = self.get_tgt_mask(mask_size=self.num_tokens, tgt_size=2)
            #tgt_pad_mask = self.create_pad_mask(y_expected, sequence_length)
            # X, y_input 및 tgt_mask를 전달하여 표준 training
            pred = self.forward(X, y_input)

            # Permute 를 수행하여 batch size 가 처음이 되도록
            pred = pred.permute(1, 0, 2)
            loss = self.loss_fn(pred, y_expected)
            if(i % 100 == 0):
                print("{} th loss : {}".format(i, loss))
            self.opt.zero_grad()
            loss.backward()
            self.opt.step()

            total_loss += loss.detach().item()

        return total_loss / len(dataLoader)

    def evaluate(self,dataloader, mode):
        return self.Pos_evaluate(dataloader, mode)

    def Pos_evaluate(self,dataloader, mode):
        self.eval()
        total_loss = 0
        count = 0
        targetdistance = 0
        distances = torch.empty(0).to(self.device)
        with torch.no_grad():
            for datas in dataloader:
                X = datas[0].to(self.device)
                y = datas[1].to(self.device)
                #tmp = torch.zeros((y.size(0), y.size(1), X.size(2) - y.size(2))).to(self.device)
                #tmp = torch.cat([y, tmp], dim=2)
                X, y = torch.tensor(X).to(self.device, dtype=torch.float32), torch.tensor(y).to(self.device,
                                                                                                  dtype=torch.float32)

                y_input = y[:, :-1]
                y_expected = y[:, 1:]

                sequence_length = y_input.size(1)
                #tgt_mask = self.get_tgt_mask(mask_size=self.num_tokens, tgt_size=2)

                pred = self.forward(X, y_input)

                pred = pred.permute(1, 0, 2)
                loss = self.loss_fn(pred, y_expected)
                total_loss += loss.detach().item()

                temp = torch.sqrt((pred[:, -1,0] - y[:, -1,0]) ** 2 + (pred[:, -1,1] - y[:, -1,1]) ** 2)
                distances = torch.cat([distances, temp], dim=0)
                targetdistance += torch.sqrt((y[:, -1, 0]) ** 2 + (y[:, -1, 1]) ** 2).sum()

                count += y.size(0)
            distances = torch.tensor(distances)
            stdDistance, meanDistance = torch.std_mean(distances)
            meanTargetDistance = targetdistance / count

        print("")
        print("{} Accracy of the model ... mean distance {} // mde : {}  std : {}".format(mode, meanTargetDistance,
                                                                                          meanDistance, stdDistance))
        return total_loss / len(dataloader)

    def get_tgt_mask(self, size) -> torch.tensor:
        mask = torch.tril(torch.ones(size, size) == 1) # Lower triangular matrix
        mask = mask.float()
        mask = mask.masked_fill(mask == 0, float('-inf')) # Convert zeros to -inf
        mask = mask.masked_fill(mask == 1, float(0.0)) # Convert ones to 0

        return mask

    def get_tgt_mask(self, mask_size, tgt_size):
        mask = torch.zeros((mask_size, mask_size))
        mask[:,0:tgt_size] = 1
        mask = mask.float()
        mask = mask.masked_fill(mask == 0, float('-inf'))
        mask = mask.masked_fill(mask==1, float(0.0))

        return mask

    def create_pad_mask(self, matrix: torch.tensor, pad_token: int) -> torch.tensor:
        return (matrix == pad_token)

class PositionalEncoding(nn.Module):
    def __init__(self, dim_model, dropout_p, max_len, device):
        super().__init__()
        # 드롭 아웃
        self.dropout = nn.Dropout(dropout_p).to(device)
        self.device = device
        # Encoding - From formula
        pos_encoding = torch.zeros(max_len, dim_model)
        positions_list = torch.arange(0, max_len, dtype=torch.float).view(-1, 1) # 0, 1, 2, 3, 4, 5
        division_term = torch.exp(torch.arange(0, dim_model, 2).float() * (-math.log(10000.0)) / dim_model) # 1000^(2i/dim_model)

        pos_encoding[:, 0::2] = torch.sin(positions_list * division_term)
        pos_encoding[:, 1::2] = torch.cos(positions_list * division_term)

        # Saving buffer (same as parameter without gradients needed)
        pos_encoding = pos_encoding.unsqueeze(0).transpose(0, 1)
        self.register_buffer("pos_encoding",pos_encoding)

    def forward(self, token_embedding):
        # Residual connection + pos encoding
        return self.dropout(token_embedding + torch.tensor(self.pos_encoding[:token_embedding.size(0), :]).to(self.device))
